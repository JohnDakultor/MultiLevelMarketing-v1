using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Domain.Services;

public sealed class RightMostPlacementStrategy : IPlacementStrategy
{
    public const string StrategyName = "RightMost";

    public string Name => StrategyName;

    public PlacementDecision SelectSlot(
        IReadOnlyCollection<PlacementCandidate> candidates,
        PlacementSide? preferredSide = null
    )
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var candidate = candidates
            .Where(node => node.HasAvailableSlot)
            .OrderByDescending(node => node.TraversalOrder)
            .ThenBy(node => node.Depth)
            .ThenBy(node => node.AgentId)
            .FirstOrDefault();

        if (candidate is null)
            throw new InvalidOperationException("No binary placement slot is available.");

        var side = candidate.RightOccupied ? PlacementSide.Left : PlacementSide.Right;
        return new PlacementDecision(candidate.AgentId, side);
    }
}
