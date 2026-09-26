using modular_mlm.Application.Organizations.Queries.ResolveOrganizationByHost.Models;

namespace modular_mlm.Application.Organizations.Queries.ResolveOrganizationByHost;

public sealed record ResolveOrganizationByHostQuery(string HostName, string? FallbackSlug = null)
    : IRequest<ResolvedOrganizationDto?>;
