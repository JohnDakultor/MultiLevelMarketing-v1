using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Commands.AdjustInventory;

public sealed class AdjustInventoryCommandHandler(
    IApplicationDbContext context,
    IAuditWriter writer,
    IUser currentUser,
    TimeProvider timeProvider
) : IRequestHandler<AdjustInventoryCommand, Guid>
{
    public async Task<Guid> Handle(
        AdjustInventoryCommand request,
        CancellationToken cancellationToken
    )
    {
        if (currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var normalizedKey = request.IdempotencyKey.Trim().ToLowerInvariant();
        var normalizedReason = request.Reason.Trim();
        var existingAdjustment = await context
            .InventoryAdjustments.AsNoTracking()
            .SingleOrDefaultAsync(
                adjustment =>
                    adjustment.OrganizationId == request.OrganizationId
                    && adjustment.IdempotencyKey == normalizedKey,
                cancellationToken
            );

        if (existingAdjustment is not null)
        {
            var isSameOperation =
                existingAdjustment.ProductVariantId == request.ProductVariantId
                && existingAdjustment.QuantityDelta == request.QuantityDelta
                && existingAdjustment.AdjustmentType == request.AdjustmentType
                && existingAdjustment.ExpectedVersion == request.ExpectedVersion
                && string.Equals(
                    existingAdjustment.Reason,
                    normalizedReason,
                    StringComparison.Ordinal
                );

            if (!isSameOperation)
                throw new IdempotencyConflictException(
                    "The idempotency key was already used for a different inventory adjustment."
                );

            return existingAdjustment.Id;
        }

        var product = await context
            .Products.Include(p => p.Variants)
            .Where(p => p.OrganizationId == request.OrganizationId)
            .SingleOrDefaultAsync(
                p => p.Variants.Any(v => v.Id == request.ProductVariantId),
                cancellationToken
            );

        if (product is null)
            throw new KeyNotFoundException("Product variant was not found in this organization.");

        var variant = product.Variants.Single(v => v.Id == request.ProductVariantId);

        if (request.ExpectedVersion != variant.Version)
            throw new InventoryConcurrencyConflictException(request.ProductVariantId);

        var balanceBefore = variant.StockQuantity;
        variant.AdjustOnHand(request.QuantityDelta);
        var balanceAfter = variant.StockQuantity;

        var adjustment = InventoryAdjustment.Record(
            request.OrganizationId,
            variant.Id,
            request.QuantityDelta,
            request.AdjustmentType,
            normalizedReason,
            normalizedKey,
            request.ExpectedVersion,
            balanceBefore,
            balanceAfter,
            timeProvider.GetUtcNow(),
            currentUser.UserId
        );
        context.InventoryAdjustments.Add(adjustment);

        var audit = AuditCoverageMap.InventoryAdjusted;
        writer.WriteAsActor(
            request.OrganizationId,
            currentUser.UserId,
            audit.Action,
            audit.EntityType,
            adjustment.Id,
            AuditJson.Serialize(
                new
                {
                    ProductVariantId = variant.Id,
                    StockQuantity = balanceBefore,
                    Version = request.ExpectedVersion,
                }
            ),
            AuditJson.Serialize(
                new
                {
                    ProductVariantId = variant.Id,
                    StockQuantity = balanceAfter,
                    variant.Version,
                    request.QuantityDelta,
                    request.AdjustmentType,
                }
            ),
            normalizedReason
        );

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConcurrencyConflictException(request.ProductVariantId);
        }

        return adjustment.Id;
    }
}
