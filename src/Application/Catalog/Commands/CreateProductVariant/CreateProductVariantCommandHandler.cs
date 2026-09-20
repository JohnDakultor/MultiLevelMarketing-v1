using FluentValidation.Results;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Commands.CreateProductVariant;

public sealed class CreateProductVariantCommandHandler(
    IApplicationDbContext db,
    IAuditWriter writer,
    IUser user,
    TimeProvider timeProvider
) : IRequestHandler<CreateProductVariantCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateProductVariantCommand request,
        CancellationToken cancellationToken
    )
    {
        var actorId = user.UserId;
        if (actorId == Guid.Empty)
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

        var sku = request.Sku.Trim().ToUpperInvariant();

        var skuExists = await (
            from existingVariant in db.ProductVariants.IgnoreQueryFilters()
            join owner in db.Products.IgnoreQueryFilters()
                on existingVariant.ProductId equals owner.Id
            where
                owner.OrganizationId == request.OrganizationId
                && existingVariant.Sku.Trim().ToUpper() == sku
            select existingVariant.Id
        ).AnyAsync(cancellationToken);

        if (skuExists)
            throw new modular_mlm.Application.Common.Exceptions.ValidationException([
                new ValidationFailure(
                    nameof(request.Sku),
                    "SKU is already used in this organization, including archived variants."
                ),
            ]);

        var variant = product.AddVariant(
            sku,
            request.Price,
            request.BusinessVolume,
            request.InitialOnHandQuantity,
            request.StockKeepingEnabled
        );
        // The generated Guid is assigned before EF discovers the new aggregate child.
        db.ProductVariants.Add(variant);

        // Zero opening stock needs no ledger movement: adjustments require a non-zero delta.
        if (variant.StockKeepingEnabled && variant.StockQuantity > 0)
        {
            db.InventoryAdjustments.Add(
                InventoryAdjustment.Record(
                    request.OrganizationId,
                    variant.Id,
                    variant.StockQuantity,
                    InventoryAdjustmentType.Receipt,
                    "Initial stock on variant creation.",
                    $"variant-initial-stock:{variant.Id:N}",
                    variant.Version,
                    0,
                    variant.StockQuantity,
                    timeProvider.GetUtcNow(),
                    actorId
                )
            );
        }

        var audit = AuditCoverageMap.ProductVariantCreated;
        writer.WriteAsActor(
            request.OrganizationId,
            actorId,
            audit.Action,
            audit.EntityType,
            variant.Id,
            null,
            AuditJson.Serialize(
                new
                {
                    variant.ProductId,
                    variant.Sku,
                    variant.Price,
                    variant.BusinessVolume,
                    variant.StockQuantity,
                    variant.StockKeepingEnabled,
                    variant.Status,
                    variant.Version,
                }
            ),
            null
        );

        await db.SaveChangesAsync(cancellationToken);
        return variant.Id;
    }
}
