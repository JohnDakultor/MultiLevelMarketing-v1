using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.ValueObjects;

/// <summary>
/// Immutable result returned by a binary placement strategy.
/// </summary>
public sealed record PlacementDecision
{
    public PlacementDecision(Guid parentAgentId, PlacementSide side)
    {
        if (parentAgentId == Guid.Empty)
            throw new DomainInvariantException("Placement parent is required.");

        ParentAgentId = parentAgentId;
        Side = side;
    }

    public Guid ParentAgentId { get; }
    public PlacementSide Side { get; }
}
