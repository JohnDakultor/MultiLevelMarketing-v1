using Microsoft.Extensions.Logging;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Commands.RemoveProductImage;

public sealed class RemoveProductImageCommandHandler(
    IApplicationDbContext db,
    IObjectStorage objectStorage,
    IAuditWriter auditWriter,
    ILogger<RemoveProductImageCommandHandler> logger
) : IRequestHandler<RemoveProductImageCommand>
{
    public async Task Handle(
        RemoveProductImageCommand request,
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
        if (product.DefaultImageUrl is null)
            return;

        var previousImageUrl = product.DefaultImageUrl;
        var previousObjectKey = product.DefaultImageObjectKey;
        product.RemoveDefaultImage();
        var audit = AuditCoverageMap.ProductImageRemoved;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            product.Id,
            AuditJson.Serialize(new { ImageUrl = previousImageUrl }),
            AuditJson.Serialize(new { ImageUrl = (string?)null }),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(previousObjectKey))
            return;
        try
        {
            await objectStorage.DeleteAsync(previousObjectKey, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to remove deleted product image object {ObjectKey}",
                previousObjectKey
            );
        }
    }
}
