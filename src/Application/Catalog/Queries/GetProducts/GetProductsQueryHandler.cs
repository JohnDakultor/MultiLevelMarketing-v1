using modular_mlm.Application.Catalog.Queries.GetProducts.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .Products.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId && x.Status == ProductStatus.Active
            )
            .OrderBy(x => x.Name)
            .Skip((Math.Max(1, request.Page) - 1) * Math.Clamp(request.PageSize, 1, 100))
            .Take(Math.Clamp(request.PageSize, 1, 100))
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.Slug,
                x.DefaultImageUrl,
                x.Variants.OrderBy(v => v.Price).Select(v => v.Price).FirstOrDefault(),
                x.Variants.OrderBy(v => v.Price).Select(v => v.BusinessVolume).FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);
}
