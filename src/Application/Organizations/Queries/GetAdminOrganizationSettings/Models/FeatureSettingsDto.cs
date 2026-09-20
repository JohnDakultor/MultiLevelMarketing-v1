namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

public sealed record FeatureSettingsDto(
    bool CommerceEnabled,
    bool AgentProgramEnabled,
    bool BinaryNetworkEnabled,
    bool BinaryPairingEnabled,
    bool WalletEnabled,
    bool PayoutEnabled,
    bool ReviewsEnabled,
    bool CouponsEnabled
);
