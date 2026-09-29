using modular_mlm.Application.Catalog.Queries.GetProducts.Models;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Queries.GetProducts;

public sealed record GetProductsQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    string? CategorySlug = null,
    decimal? MinimumPrice = null,
    decimal? MaximumPrice = null,
    ProductAvailability Availability = ProductAvailability.Any,
    ProductSort Sort = ProductSort.Newest
)
    : IRequest<PagedResponse<ProductDto>>,
        ICacheableRequest
{
    public string CacheKey =>
        $"catalog:{OrganizationId:N}:products:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{CategoryId}:{CategorySlug?.Trim().ToLowerInvariant()}:{MinimumPrice}:{MaximumPrice}:{Availability}:{Sort}";
    public TimeSpan CacheLifetime => TimeSpan.FromMinutes(2);
}

public enum ProductAvailability
{
    Any,
    InStock,
    OutOfStock,
}

public enum ProductSort
{
    Newest,
    PriceAscending,
    PriceDescending,
    NameAscending,
    NameDescending,
}
