namespace modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig.Models;

public sealed record PublicOrganizationConfigDto(
    Guid Id,
    string Name,
    string Slug,
    string CurrencyCode,
    string Locale,
    string StoreTitle,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string? LogoUrl,
    bool AgentProgramEnabled,
    bool BinaryNetworkEnabled,
    bool WalletEnabled,
    bool PayoutEnabled
);
