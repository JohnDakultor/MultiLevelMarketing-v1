using modular_mlm.Application.Inventory.Queries.GetInventoryHistory.Models;

namespace modular_mlm.Application.Inventory.Queries.GetInventoryHistory;

public sealed class GetInventoryHistoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInventoryHistoryQuery, InventoryHistoryPageDto>
{
    public async Task<InventoryHistoryPageDto> Handle(
        GetInventoryHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        var variantExists = await (
            from variant in db.ProductVariants.AsNoTracking()
            join product in db.Products.AsNoTracking() on variant.ProductId equals product.Id
            where
                variant.Id == request.ProductVariantId
                && product.OrganizationId == request.OrganizationId
            select variant.Id
        ).AnyAsync(cancellationToken);
        if (!variantExists)
            throw new KeyNotFoundException("Product variant was not found in this organization.");

        var query = db
            .InventoryAdjustments.AsNoTracking()
            .Where(adjustment =>
                adjustment.OrganizationId == request.OrganizationId
                && adjustment.ProductVariantId == request.ProductVariantId
            );
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(adjustment => adjustment.OccurredAt)
            .ThenByDescending(adjustment => adjustment.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(adjustment => new InventoryHistoryItemDto(
                adjustment.Id,
                adjustment.AdjustmentType,
                adjustment.QuantityDelta,
                adjustment.BalanceBefore,
                adjustment.BalanceAfter,
                adjustment.Reason,
                adjustment.ActorUserId,
                adjustment.OccurredAt
            ))
            .ToListAsync(cancellationToken);

        return new InventoryHistoryPageDto(
            request.ProductVariantId,
            items,
            request.Page,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)
        );
    }
}
