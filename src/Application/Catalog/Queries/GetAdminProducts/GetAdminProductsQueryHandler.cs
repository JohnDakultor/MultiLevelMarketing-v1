using modular_mlm.Application.Catalog.Queries.GetAdminProducts.Models;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Queries.GetAdminProducts;

public sealed class GetAdminProductsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminProductsQuery, IReadOnlyList<AdminProductDto>>
{
    public async Task<IReadOnlyList<AdminProductDto>> Handle(
        GetAdminProductsQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .Products.AsNoTracking()
            .Where(product => product.OrganizationId == request.OrganizationId)
            .OrderBy(product => product.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product => new AdminProductDto(
                product.Id,
                product.CategoryId,
                db.Categories.Where(category => category.Id == product.CategoryId)
                    .Select(category => category.Name)
                    .FirstOrDefault()
                    ?? "Unknown category",
                product.Name,
                product.Slug,
                product.Status,
                product
                    .Variants.OrderBy(variant => variant.Price)
                    .Select(variant => variant.Price)
                    .FirstOrDefault(),
                product
                    .Variants.OrderBy(variant => variant.Price)
                    .Select(variant => variant.BusinessVolume)
                    .FirstOrDefault(),
                product.Variants.Sum(variant => variant.StockQuantity)
            ))
            .ToListAsync(cancellationToken);
}
