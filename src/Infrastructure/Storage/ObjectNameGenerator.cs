using System.Security.Cryptography;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Models;

namespace modular_mlm.Infrastructure.Storage;

public sealed class ObjectNameGenerator : IObjectNameGenerator
{
    private static readonly HashSet<string> AllowedExtensions = new(
        ["png", "jpg", "ico"],
        StringComparer.Ordinal
    );

    public string CreateBrandingObjectKey(
        Guid organizationId,
        BrandingAssetKind assetKind,
        string safeExtension
    )
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization is required.", nameof(organizationId));
        if (!Enum.IsDefined(assetKind))
            throw new ArgumentOutOfRangeException(nameof(assetKind));

        var extension = safeExtension.Trim().TrimStart('.').ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("File extension is not supported.", nameof(safeExtension));

        var kind = assetKind.ToString().ToLowerInvariant();
        var identifier = RandomNumberGenerator.GetHexString(16).ToLowerInvariant();
        return $"organizations/{organizationId:D}/branding/{kind}/{identifier}.{extension}";
    }
}
