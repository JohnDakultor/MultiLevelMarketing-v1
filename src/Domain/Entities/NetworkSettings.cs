using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Organizations;

public sealed class NetworkSettings
{
    private NetworkSettings() { }

    public PlacementStrategyType DefaultPlacementStrategy { get; private set; }
    public bool AllowAgentPreferredLeg { get; private set; }
    public int MaxQueryDepth { get; private set; }
    public bool AutoPlacementEnabled { get; private set; }
    public bool RestrictPlacementChangesAfterActivation { get; private set; }

    public static NetworkSettings Default() =>
        Create(PlacementStrategyType.BreadthFirst, true, 10, true, true);

    public static NetworkSettings Create(
        PlacementStrategyType defaultPlacementStrategy,
        bool allowAgentPreferredLeg,
        int maxQueryDepth,
        bool autoPlacementEnabled,
        bool restrictPlacementChangesAfterActivation
    )
    {
        if (!Enum.IsDefined(defaultPlacementStrategy))
            throw new DomainInvariantException("Placement strategy is invalid.");
        if (maxQueryDepth is < 1 or > 100)
            throw new DomainInvariantException("Network query depth must be between 1 and 100.");

        return new NetworkSettings
        {
            DefaultPlacementStrategy = defaultPlacementStrategy,
            AllowAgentPreferredLeg = allowAgentPreferredLeg,
            MaxQueryDepth = maxQueryDepth,
            AutoPlacementEnabled = autoPlacementEnabled,
            RestrictPlacementChangesAfterActivation = restrictPlacementChangesAfterActivation,
        };
    }
}
