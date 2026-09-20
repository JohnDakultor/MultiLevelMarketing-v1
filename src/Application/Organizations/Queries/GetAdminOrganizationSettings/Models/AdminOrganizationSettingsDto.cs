namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

public sealed record AdminOrganizationSettingsDto(
    OrganizationProfileDto Profile,
    BrandingSettingsDto Branding,
    FeatureSettingsDto Features,
    CommerceSettingsDto Commerce,
    NetworkSettingsDto Network,
    IReadOnlyList<OrganizationDomainDto> Domains
);
