using modular_mlm.Application.Inventory.Queries.GetInventory.Models;

namespace modular_mlm.Application.Inventory.Queries.GetInventory;

public sealed class GetInventoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInventoryQuery, InventoryPageDto>
{
    private const int LowStockThreshold = 10;

    public async Task<InventoryPageDto> Handle(
        GetInventoryQuery request,
        CancellationToken cancellationToken
    )
    {
        var query =
            from variant in db.ProductVariants.AsNoTracking()
            join product in db.Products.AsNoTracking() on variant.ProductId equals product.Id
            where product.OrganizationId == request.OrganizationId
            select new { Product = product, Variant = variant };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(row =>
                row.Product.Name.ToLower().Contains(search)
                || row.Variant.Sku.ToLower().Contains(search)
            );
        }
        if (request.Status.HasValue)
            query = query.Where(row => row.Variant.Status == request.Status.Value);
        if (request.LowStockOnly)
            query = query.Where(row =>
                row.Variant.StockKeepingEnabled
                && row.Variant.StockQuantity - row.Variant.ReservedQuantity <= LowStockThreshold
            );

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(row => row.Product.Name)
            .ThenBy(row => row.Variant.Sku)
            .ThenBy(row => row.Variant.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(row => new InventoryItemDto(
                row.Product.Id,
                row.Product.Name,
                row.Product.Status,
                row.Variant.Id,
                row.Variant.Sku,
                row.Variant.Status,
                row.Variant.StockKeepingEnabled,
                row.Variant.StockQuantity,
                row.Variant.ReservedQuantity,
                row.Variant.StockKeepingEnabled
                    ? row.Variant.StockQuantity - row.Variant.ReservedQuantity
                    : null,
                row.Variant.Version,
                row.Variant.LastModified
            ))
            .ToListAsync(cancellationToken);

        return new InventoryPageDto(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)
        );
    }
}
