using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;

namespace modular_mlm.Application.Catalog.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    IUser user
) : IRequestHandler<UpdateProductVariantCommand, Guid>
{
    public async Task<Guid> Handle(
        UpdateProductVariantCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = user.UserId;
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var product = await db
            .Products.Include(candidate => candidate.Variants)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.ProductId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (product is null)
            throw new KeyNotFoundException("Product was not found in this organization.");

        var variant = product.Variants.SingleOrDefault(candidate =>
            candidate.Id == request.ProductVariantId
        );
        if (variant is null)
            throw new KeyNotFoundException("Product variant was not found in this product.");
        if (variant.Version != request.ExpectedVersion)
            throw new InventoryConcurrencyConflictException(variant.Id);

        var before = AuditJson.Serialize(
            new
            {
                variant.Price,
                variant.BusinessVolume,
                variant.Weight,
                variant.AttributesJson,
                variant.StockKeepingEnabled,
                variant.Version,
            }
        );
        variant.UpdateDetails(
            request.Price,
            request.BusinessVolume,
            request.Weight,
            request.AttributesJson,
            request.StockKeepingEnabled
        );

        var audit = AuditCoverageMap.ProductVariantUpdated;
        auditWriter.WriteAsActor(
            request.OrganizationId,
            userId,
            audit.Action,
            audit.EntityType,
            variant.Id,
            before,
            AuditJson.Serialize(
                new
                {
                    variant.Price,
                    variant.BusinessVolume,
                    variant.Weight,
                    variant.AttributesJson,
                    variant.StockKeepingEnabled,
                    variant.Version,
                }
            ),
            null
        );

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return variant.Id;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConcurrencyConflictException(variant.Id);
        }
    }
}
