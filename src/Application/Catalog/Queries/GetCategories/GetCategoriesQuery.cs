using modular_mlm.Application.Catalog.Queries.GetCategories.Models;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Queries.GetCategories;

public sealed record GetCategoriesQuery(Guid OrganizationId)
    : IRequest<IReadOnlyList<CategoryDto>>,
        ICacheableRequest
{
    public string CacheKey => $"catalog:{OrganizationId:N}:categories";
    public TimeSpan CacheLifetime => TimeSpan.FromMinutes(5);
}
