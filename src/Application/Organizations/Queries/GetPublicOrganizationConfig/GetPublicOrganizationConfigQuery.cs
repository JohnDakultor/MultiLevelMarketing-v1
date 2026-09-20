using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig.Models;

namespace modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig;

public sealed record GetPublicOrganizationConfigQuery(string Slug)
    : IRequest<PublicOrganizationConfigDto?>,
        ICacheableRequest
{
    public string CacheKey => $"public-config:{Slug.Trim().ToLowerInvariant()}";
    public TimeSpan CacheLifetime => TimeSpan.FromMinutes(5);
}
