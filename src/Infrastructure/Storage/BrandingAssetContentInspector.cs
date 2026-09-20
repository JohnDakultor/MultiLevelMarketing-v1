using System.Buffers.Binary;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Models;

namespace modular_mlm.Infrastructure.Storage;

public sealed class BrandingAssetContentInspector : IBrandingAssetContentInspector
{
    private const int MaximumLogoDimension = 4_096;
    private const int MaximumFaviconDimension = 512;

    public BrandingAssetInspection Inspect(
        ReadOnlyMemory<byte> content,
        string fileName,
        string declaredContentType,
        BrandingAssetKind assetKind
    )
    {
        if (content.IsEmpty || content.Length > BrandingAssetLimits.MaximumContentLength)
            throw new InvalidDataException("Branding asset size is invalid.");
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidDataException("Branding asset filename is required.");
        if (!Enum.IsDefined(assetKind))
            throw new InvalidDataException("Branding asset kind is invalid.");

        var detected = Detect(content.Span);
        var declared = declaredContentType.Trim().ToLowerInvariant();
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (declared != detected.ContentType)
            throw new InvalidDataException("Declared MIME type does not match file content.");
        if (!detected.AcceptedExtensions.Contains(extension, StringComparer.Ordinal))
            throw new InvalidDataException("File extension does not match file content.");

        var maximumDimension =
            assetKind == BrandingAssetKind.Logo ? MaximumLogoDimension : MaximumFaviconDimension;
        if (detected.Width is < 1 || detected.Height is < 1)
            throw new InvalidDataException("Image dimensions are invalid.");
        if (detected.Width > maximumDimension || detected.Height > maximumDimension)
            throw new InvalidDataException("Image dimensions exceed the allowed maximum.");
        if (assetKind == BrandingAssetKind.Favicon && detected.Extension == "jpg")
            throw new InvalidDataException("Favicons must use PNG or ICO format.");

        return new BrandingAssetInspection(
            detected.ContentType,
            detected.Extension,
            detected.Width,
            detected.Height
        );
    }

    private static DetectedImage Detect(ReadOnlySpan<byte> content)
    {
        if (
            content.Length >= 24
            && content[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && content.Slice(12, 4).SequenceEqual("IHDR"u8)
        )
        {
            return new DetectedImage(
                "image/png",
                "png",
                ["png"],
                checked((int)BinaryPrimitives.ReadUInt32BigEndian(content.Slice(16, 4))),
                checked((int)BinaryPrimitives.ReadUInt32BigEndian(content.Slice(20, 4)))
            );
        }

        if (
            content.Length >= 22
            && content[0] == 0
            && content[1] == 0
            && content[2] == 1
            && content[3] == 0
            && BinaryPrimitives.ReadUInt16LittleEndian(content.Slice(4, 2)) > 0
        )
        {
            return new DetectedImage(
                "image/x-icon",
                "ico",
                ["ico"],
                content[6] == 0 ? 256 : content[6],
                content[7] == 0 ? 256 : content[7]
            );
        }

        if (content.Length >= 4 && content[0] == 0xFF && content[1] == 0xD8)
            return DetectJpeg(content);

        throw new InvalidDataException("Only PNG, JPEG, and ICO images are supported.");
    }

    private static DetectedImage DetectJpeg(ReadOnlySpan<byte> content)
    {
        var offset = 2;
        while (offset < content.Length)
        {
            if (content[offset++] != 0xFF)
                continue;

            while (offset < content.Length && content[offset] == 0xFF)
                offset++;
            if (offset >= content.Length)
                break;

            var marker = content[offset++];
            if (marker is 0xD8 or 0xD9 || marker is >= 0xD0 and <= 0xD7)
                continue;
            if (marker == 0xDA || offset + 2 > content.Length)
                break;

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(content.Slice(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > content.Length)
                throw new InvalidDataException("JPEG segment is invalid.");

            if (IsStartOfFrame(marker))
            {
                if (segmentLength < 7)
                    throw new InvalidDataException("JPEG dimensions are missing.");

                var height = BinaryPrimitives.ReadUInt16BigEndian(content.Slice(offset + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(content.Slice(offset + 5, 2));
                return new DetectedImage("image/jpeg", "jpg", ["jpg", "jpeg"], width, height);
            }

            offset += segmentLength;
        }

        throw new InvalidDataException("JPEG dimensions could not be determined.");
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker
            is 0xC0
                or 0xC1
                or 0xC2
                or 0xC3
                or 0xC5
                or 0xC6
                or 0xC7
                or 0xC9
                or 0xCA
                or 0xCB
                or 0xCD
                or 0xCE
                or 0xCF;

    private sealed record DetectedImage(
        string ContentType,
        string Extension,
        string[] AcceptedExtensions,
        int Width,
        int Height
    );
}
