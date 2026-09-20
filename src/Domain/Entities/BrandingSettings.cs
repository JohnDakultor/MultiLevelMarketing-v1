using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Organizations;

public sealed class BrandingSettings
{
    private BrandingSettings() { }

    public string? LogoUrl { get; private set; }
    public string? FaviconUrl { get; private set; }
    public string PrimaryColor { get; private set; } = "#2563EB";
    public string SecondaryColor { get; private set; } = "#0F172A";
    public string AccentColor { get; private set; } = "#22C55E";
    public string StoreTitle { get; private set; } = string.Empty;
    public string SupportEmail { get; private set; } = string.Empty;
    public string? SupportPhone { get; private set; }
    public string? FooterText { get; private set; }

    public static BrandingSettings Create(
        string storeTitle,
        string supportEmail,
        string primaryColor = "#2563EB",
        string secondaryColor = "#0F172A",
        string accentColor = "#22C55E",
        string? logoUrl = null,
        string? faviconUrl = null,
        string? supportPhone = null,
        string? footerText = null
    )
    {
        if (string.IsNullOrWhiteSpace(storeTitle))
            throw new DomainInvariantException("Store title is required.");
        return new BrandingSettings
        {
            StoreTitle = storeTitle.Trim(),
            SupportEmail = supportEmail.Trim(),
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor,
            AccentColor = accentColor,
            LogoUrl = logoUrl,
            FaviconUrl = faviconUrl,
            SupportPhone = supportPhone,
            FooterText = footerText,
        };
    }
}
