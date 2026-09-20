namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

public sealed record BrandingSettingsDto(
    string? LogoUrl,
    string? FaviconUrl,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string StoreTitle,
    string SupportEmail,
    string? SupportPhone,
    string? FooterText,
    int Revision,
    int PublishedRevision,
    DateTimeOffset? PublishedAt
);
