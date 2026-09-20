using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Organizations.Commands.UpdateNetworkSettings;

public sealed record UpdateNetworkSettingsCommand(
    Guid OrganizationId,
    PlacementStrategyType DefaultPlacementStrategy,
    bool AllowAgentPreferredLeg,
    int MaxQueryDepth,
    bool AutoPlacementEnabled,
    bool RestrictPlacementChangesAfterActivation
) : IRequest, IOrganizationAdminRequest;
