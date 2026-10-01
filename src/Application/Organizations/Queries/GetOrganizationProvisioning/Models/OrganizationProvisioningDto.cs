namespace modular_mlm.Application.Organizations.Queries.GetOrganizationProvisioning.Models;

public sealed record OrganizationProvisioningDto(
    Guid OrganizationId,
    string Name,
    string Slug,
    bool BrandingPublished
);
