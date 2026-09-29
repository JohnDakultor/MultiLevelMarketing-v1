using Microsoft.Extensions.Logging;
using modular_mlm.Application.Catalog.Models;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Catalog.Commands.UploadProductImage;

public sealed class UploadProductImageCommandHandler(
    IApplicationDbContext db,
    IObjectStorage objectStorage,
    IProductImageContentInspector contentInspector,
    IObjectNameGenerator objectNameGenerator,
    IAuditWriter auditWriter,
    ILogger<UploadProductImageCommandHandler> logger
) : IRequestHandler<UploadProductImageCommand, StoredObject>
{
    public async Task<StoredObject> Handle(
        UploadProductImageCommand request,
        CancellationToken cancellationToken
    )
    {
        var product = await db.Products.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.ProductId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (product is null)
            throw new KeyNotFoundException("Product was not found in this organization.");

        var content = await ReadBoundedContentAsync(request, cancellationToken);
        var inspection = contentInspector.Inspect(
            content,
            request.FileName,
            request.ContentType
        );
        var objectKey = objectNameGenerator.CreateProductImageObjectKey(
            request.OrganizationId,
            product.Id,
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

        var previousObjectKey = product.DefaultImageObjectKey;
        var previousImageUrl = product.DefaultImageUrl;
        try
        {
            product.SetDefaultImage(storedObject.Url.AbsoluteUri, storedObject.ObjectKey);
            var audit = AuditCoverageMap.ProductImageUploaded;
            auditWriter.Write(
                request.OrganizationId,
                audit.Action,
                audit.EntityType,
                product.Id,
                AuditJson.Serialize(new { ImageUrl = previousImageUrl }),
                AuditJson.Serialize(new { ImageUrl = product.DefaultImageUrl }),
                reason: null
            );
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(storedObject.ObjectKey, "new", CancellationToken.None);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousObjectKey))
            await TryDeleteAsync(previousObjectKey, "replaced", CancellationToken.None);

        return storedObject;
    }

    private static async Task<byte[]> ReadBoundedContentAsync(
        UploadProductImageCommand request,
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

    private async Task TryDeleteAsync(
        string objectKey,
        string disposition,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await objectStorage.DeleteAsync(objectKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to remove {Disposition} product image object {ObjectKey}",
                disposition,
                objectKey
            );
        }
    }
}
