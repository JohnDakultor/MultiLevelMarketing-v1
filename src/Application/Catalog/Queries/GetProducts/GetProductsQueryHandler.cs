using modular_mlm.Application.Catalog.Queries.GetProducts.Models;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductsQuery, PagedResponse<ProductDto>>
{
    public async Task<PagedResponse<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = db
            .Products.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId && x.Status == ProductStatus.Active
            );

        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.ToLower().Contains(search)
                || x.Description.ToLower().Contains(search)
                || (x.Brand != null && x.Brand.ToLower().Contains(search))
            );
        }

        if (request.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);

        var categorySlug = request.CategorySlug?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(categorySlug))
            query = query.Where(product =>
                db.Categories.Any(category =>
                    category.OrganizationId == request.OrganizationId
                    && category.Id == product.CategoryId
                    && category.IsActive
                    && category.Slug == categorySlug
                )
            );

        if (request.MinimumPrice.HasValue)
            query = query.Where(x => x.Variants.Min(variant => variant.Price) >= request.MinimumPrice.Value);
        if (request.MaximumPrice.HasValue)
            query = query.Where(x => x.Variants.Min(variant => variant.Price) <= request.MaximumPrice.Value);

        if (request.Availability != ProductAvailability.Any)
        {
            query = query.Where(product =>
                request.Availability == ProductAvailability.InStock
                    ? product.Variants.Any(variant =>
                        variant.Status != ProductStatus.Archived
                        && (!variant.StockKeepingEnabled || variant.StockQuantity > variant.ReservedQuantity)
                    )
                    : !product.Variants.Any(variant =>
                        variant.Status != ProductStatus.Archived
                        && (!variant.StockKeepingEnabled || variant.StockQuantity > variant.ReservedQuantity)
                    )
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = request.Sort switch
        {
            ProductSort.PriceAscending => query.OrderBy(x => x.Variants.Min(v => v.Price)).ThenBy(x => x.Name),
            ProductSort.PriceDescending => query.OrderByDescending(x => x.Variants.Min(v => v.Price)).ThenBy(x => x.Name),
            ProductSort.NameAscending => query.OrderBy(x => x.Name).ThenBy(x => x.Id),
            ProductSort.NameDescending => query.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Created).ThenByDescending(x => x.Id),
        };
        var items = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.Slug,
                x.DefaultImageUrl,
                x.Variants.OrderBy(v => v.Price).Select(v => v.Price).FirstOrDefault(),
                x.Variants.OrderBy(v => v.Price).Select(v => v.BusinessVolume).FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductDto>(items, request.Page, request.PageSize, totalCount);
    }
}
