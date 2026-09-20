using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Domain.Services;

/// <summary>
/// Keeps the placement tree compact while preferring the locally smaller available leg.
/// </summary>
public sealed class BalancedLegPlacementStrategy : IPlacementStrategy
{
    public const string StrategyName = "BalancedLeg";

    public string Name => StrategyName;

    public PlacementDecision SelectSlot(
        IReadOnlyCollection<PlacementCandidate> candidates,
        PlacementSide? preferredSide = null
    )
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var selection = candidates
            .Where(candidate => candidate.HasAvailableSlot)
            .Select(candidate => CreateSelection(candidate, preferredSide))
            .OrderBy(candidate => candidate.Candidate.Depth)
            .ThenBy(candidate => candidate.SelectedLegSize)
            .ThenBy(candidate => candidate.ProjectedImbalance)
            .ThenBy(candidate => candidate.Candidate.TraversalOrder)
            .ThenBy(candidate => candidate.Candidate.AgentId)
            .FirstOrDefault();

        if (selection is null)
            throw new InvalidOperationException("No binary placement slot is available.");

        return new PlacementDecision(selection.Candidate.AgentId, selection.Side);
    }

    private static CandidateSelection CreateSelection(
        PlacementCandidate candidate,
        PlacementSide? preferredSide
    )
    {
        var side = SelectSide(candidate, preferredSide);
        var selectedLegSize =
            side == PlacementSide.Left ? candidate.LeftLegSize : candidate.RightLegSize;
        var projectedLeftSize = candidate.LeftLegSize + (side == PlacementSide.Left ? 1 : 0);
        var projectedRightSize = candidate.RightLegSize + (side == PlacementSide.Right ? 1 : 0);

        return new CandidateSelection(
            candidate,
            side,
            selectedLegSize,
            Math.Abs(projectedLeftSize - projectedRightSize)
        );
    }

    private static PlacementSide SelectSide(
        PlacementCandidate candidate,
        PlacementSide? preferredSide
    )
    {
        if (candidate.LeftOccupied)
            return PlacementSide.Right;
        if (candidate.RightOccupied)
            return PlacementSide.Left;

        if (candidate.LeftLegSize < candidate.RightLegSize)
            return PlacementSide.Left;
        if (candidate.RightLegSize < candidate.LeftLegSize)
            return PlacementSide.Right;

        return preferredSide ?? PlacementSide.Left;
    }

    private sealed record CandidateSelection(
        PlacementCandidate Candidate,
        PlacementSide Side,
        int SelectedLegSize,
        int ProjectedImbalance
    );
}
