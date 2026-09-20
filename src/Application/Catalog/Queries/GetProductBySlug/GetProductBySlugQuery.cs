using modular_mlm.Application.Catalog.Queries.GetProductBySlug.Models;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Queries.GetProductBySlug;

public sealed record GetProductBySlugQuery(Guid OrganizationId, string Slug)
    : IRequest<ProductDetailDto>,
        ICacheableRequest
{
    public string CacheKey =>
        $"catalog:{OrganizationId:N}:product:{Slug.Trim().ToLowerInvariant()}";
    public TimeSpan CacheLifetime => TimeSpan.FromMinutes(2);
}
