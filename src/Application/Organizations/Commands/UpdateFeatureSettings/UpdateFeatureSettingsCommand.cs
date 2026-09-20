namespace modular_mlm.Application.Organizations.Commands.UpdateFeatureSettings;

public sealed record UpdateFeatureSettingsCommand(
    Guid OrganizationId,
    bool CommerceEnabled,
    bool AgentProgramEnabled,
    bool BinaryNetworkEnabled,
    bool BinaryPairingEnabled,
    bool WalletEnabled,
    bool PayoutEnabled
) : IRequest, IOrganizationAdminRequest;
