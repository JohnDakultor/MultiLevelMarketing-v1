namespace modular_mlm.Domain.Network;

/// <summary>
/// Placement policies supported by the binary network.
/// </summary>
public enum PlacementStrategyType
{
    BreadthFirst,
    PreferredLeg,
    LeftMost,
    RightMost,
    BalancedLeg,
}
