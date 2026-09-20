namespace modular_mlm.Application.Organizations.Commands.UpdateBranding;

public sealed record UpdateBrandingCommand(
    Guid OrganizationId,
    string StoreTitle,
    string SupportEmail,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string? LogoUrl,
    string? FaviconUrl,
    string? SupportPhone,
    string? FooterText
) : IRequest, IOrganizationAdminRequest;
