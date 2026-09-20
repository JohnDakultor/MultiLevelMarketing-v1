using modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig.Models;

namespace modular_mlm.Application.Organizations.Queries.ResolveOrganizationByHost.Models;

public sealed record ResolvedOrganizationDto(
    Guid OrganizationId,
    string Slug,
    string HostName,
    PublicOrganizationConfigDto PublicConfig
);
