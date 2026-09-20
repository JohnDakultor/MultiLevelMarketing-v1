using Microsoft.Extensions.Logging;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Organizations.Models;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UploadBrandingAsset;

public sealed class UploadBrandingAssetCommandHandler(
    IApplicationDbContext db,
    IObjectStorage objectStorage,
    IBrandingAssetContentInspector contentInspector,
    IObjectNameGenerator objectNameGenerator,
    IAuditWriter auditWriter,
    ILogger<UploadBrandingAssetCommandHandler> logger
) : IRequestHandler<UploadBrandingAssetCommand, StoredObject>
{
    public async Task<StoredObject> Handle(
        UploadBrandingAssetCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            candidate => candidate.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var content = await ReadBoundedContentAsync(request, cancellationToken);
        var inspection = contentInspector.Inspect(
            content,
            request.FileName,
            request.ContentType,
            request.AssetKind
        );
        var objectKey = objectNameGenerator.CreateBrandingObjectKey(
            organization.Id,
            request.AssetKind,
            inspection.Extension
        );

        await using var uploadStream = new MemoryStream(content, writable: false);
        var storedObject = await objectStorage.PutAsync(
            new ObjectUploadRequest(
                objectKey,
                inspection.ContentType,
                content.LongLength,
                uploadStream
            ),
            cancellationToken
        );

        try
        {
            var beforeJson = Serialize(organization.Branding);
            organization.UpdateBranding(
                BrandingSettings.Create(
                    organization.Branding.StoreTitle,
                    organization.Branding.SupportEmail,
                    organization.Branding.PrimaryColor,
                    organization.Branding.SecondaryColor,
                    organization.Branding.AccentColor,
                    request.AssetKind == BrandingAssetKind.Logo
                        ? storedObject.Url.AbsoluteUri
                        : organization.Branding.LogoUrl,
                    request.AssetKind == BrandingAssetKind.Favicon
                        ? storedObject.Url.AbsoluteUri
                        : organization.Branding.FaviconUrl,
                    organization.Branding.SupportPhone,
                    organization.Branding.FooterText
                )
            );

            var audit = AuditCoverageMap.BrandingAssetUploaded;
            auditWriter.Write(
                organization.Id,
                audit.Action,
                audit.EntityType,
                organization.Id,
                beforeJson,
                Serialize(organization.Branding),
                reason: null
            );
            await db.SaveChangesAsync(cancellationToken);
            return storedObject;
        }
        catch
        {
            try
            {
                await objectStorage.DeleteAsync(storedObject.ObjectKey, CancellationToken.None);
            }
            catch (Exception compensationException)
            {
                logger.LogError(
                    compensationException,
                    "Failed to remove orphaned branding object {ObjectKey}",
                    storedObject.ObjectKey
                );
            }

            throw;
        }
    }

    private static async Task<byte[]> ReadBoundedContentAsync(
        UploadBrandingAssetCommand request,
        CancellationToken cancellationToken
    )
    {
        var expectedLength = checked((int)request.ContentLength);
        var buffer = new byte[expectedLength + 1];
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await request.Content.ReadAsync(
                buffer.AsMemory(totalRead, buffer.Length - totalRead),
                cancellationToken
            );
            if (read == 0)
                break;
            totalRead += read;
        }

        if (totalRead != expectedLength)
            throw new InvalidDataException(
                "Uploaded content length does not match its declaration."
            );

        return buffer[..totalRead];
    }

    private static string Serialize(BrandingSettings branding) =>
        AuditJson.Serialize(
            new
            {
                branding.LogoUrl,
                branding.FaviconUrl,
                branding.PrimaryColor,
                branding.SecondaryColor,
                branding.AccentColor,
                branding.StoreTitle,
                branding.SupportEmail,
                branding.SupportPhone,
                branding.FooterText,
            }
        );
}
