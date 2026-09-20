using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Domain.Services;

/// <summary>
/// Selects the first available binary slot in breadth-first, left-to-right order.
/// </summary>
public sealed class BreadthFirstPlacementStrategy : IPlacementStrategy
{
    public const string StrategyName = "BreadthFirst";

    public string Name => StrategyName;

    public PlacementDecision SelectSlot(
        IReadOnlyCollection<PlacementCandidate> candidates,
        PlacementSide? preferredSide = null
    )
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var candidate = candidates
            .Where(node => node.HasAvailableSlot)
            .OrderBy(node => node.Depth)
            .ThenBy(node => node.TraversalOrder)
            .ThenBy(node => node.AgentId)
            .FirstOrDefault();

        if (candidate is null)
            throw new InvalidOperationException("No binary placement slot is available.");

        var side = candidate.LeftOccupied ? PlacementSide.Right : PlacementSide.Left;
        return new PlacementDecision(candidate.AgentId, side);
    }
}
