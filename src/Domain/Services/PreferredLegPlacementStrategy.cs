using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Domain.Services;

/// <summary>
/// Searches breadth-first for the requested side and falls back to the first available slot.
/// </summary>
public sealed class PreferredLegPlacementStrategy : IPlacementStrategy
{
    public const string StrategyName = "PreferredLeg";

    public string Name => StrategyName;

    public PlacementDecision SelectSlot(
        IReadOnlyCollection<PlacementCandidate> candidates,
        PlacementSide? preferredSide = null
    )
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var available = candidates.Where(candidate => candidate.HasAvailableSlot).ToArray();
        if (available.Length == 0)
            throw new InvalidOperationException("No binary placement slot is available.");

        if (preferredSide is not null)
        {
            var preferredCandidate = OrderBreadthFirst(
                    available.Where(candidate => IsAvailable(candidate, preferredSide.Value))
                )
                .FirstOrDefault();

            if (preferredCandidate is not null)
                return new PlacementDecision(preferredCandidate.AgentId, preferredSide.Value);
        }

        var fallback = OrderBreadthFirst(available).First();
        var fallbackSide = fallback.LeftOccupied ? PlacementSide.Right : PlacementSide.Left;
        return new PlacementDecision(fallback.AgentId, fallbackSide);
    }

    private static IOrderedEnumerable<PlacementCandidate> OrderBreadthFirst(
        IEnumerable<PlacementCandidate> candidates
    ) =>
        candidates
            .OrderBy(candidate => candidate.Depth)
            .ThenBy(candidate => candidate.TraversalOrder)
            .ThenBy(candidate => candidate.AgentId);

    private static bool IsAvailable(PlacementCandidate candidate, PlacementSide side) =>
        side == PlacementSide.Left ? !candidate.LeftOccupied : !candidate.RightOccupied;
}
