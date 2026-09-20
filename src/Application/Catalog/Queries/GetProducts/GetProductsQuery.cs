using modular_mlm.Application.Catalog.Queries.GetProducts.Models;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Queries.GetProducts;

public sealed record GetProductsQuery(Guid OrganizationId, int Page = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<ProductDto>>,
        ICacheableRequest
{
    public string CacheKey => $"catalog:{OrganizationId:N}:products:{Page}:{PageSize}";
    public TimeSpan CacheLifetime => TimeSpan.FromMinutes(2);
}
