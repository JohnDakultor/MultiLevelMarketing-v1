using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

public sealed record NetworkSettingsDto(
    PlacementStrategyType DefaultPlacementStrategy,
    bool AllowAgentPreferredLeg,
    int MaxQueryDepth,
    bool AutoPlacementEnabled,
    bool RestrictPlacementChangesAfterActivation
);
