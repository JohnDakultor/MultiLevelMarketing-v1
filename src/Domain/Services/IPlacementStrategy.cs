using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Domain.Services;

/// <summary>
/// Selects an available parent and side from a snapshot of the binary placement tree.
/// Implementations must be deterministic and must not access persistence or mutate agents.
/// </summary>
public interface IPlacementStrategy
{
    /// <summary>
    /// A stable identifier used by compensation-plan and organization configuration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Selects an available binary slot from the supplied candidates.
    /// </summary>
    /// <param name="candidates">
    /// Candidate nodes visible to the placement operation. The collection may be unordered;
    /// strategies must use candidate depth and traversal order when ordering matters.
    /// </param>
    /// <param name="preferredSide">
    /// Optional leg preference. Strategies that do not support a preference may ignore it.
    /// </param>
    /// <returns>The selected placement parent and side.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no candidate contains an available binary slot.
    /// </exception>
    PlacementDecision SelectSlot(
        IReadOnlyCollection<PlacementCandidate> candidates,
        PlacementSide? preferredSide = null
    );
}
