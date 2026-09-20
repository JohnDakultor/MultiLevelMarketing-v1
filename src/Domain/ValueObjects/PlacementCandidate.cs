using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.ValueObjects;

/// <summary>
/// Immutable, persistence-independent projection of a node considered for placement.
/// </summary>
public sealed record PlacementCandidate
{
    public PlacementCandidate(
        Guid agentId,
        int depth,
        long traversalOrder,
        bool leftOccupied,
        bool rightOccupied,
        int leftLegSize = 0,
        int rightLegSize = 0
    )
    {
        if (agentId == Guid.Empty)
            throw new DomainInvariantException("Placement candidate agent is required.");
        if (depth < 0)
            throw new DomainInvariantException("Placement candidate depth cannot be negative.");
        if (traversalOrder < 0)
            throw new DomainInvariantException("Traversal order cannot be negative.");
        if (leftLegSize < 0 || rightLegSize < 0)
            throw new DomainInvariantException("Leg sizes cannot be negative.");

        AgentId = agentId;
        Depth = depth;
        TraversalOrder = traversalOrder;
        LeftOccupied = leftOccupied;
        RightOccupied = rightOccupied;
        LeftLegSize = leftLegSize;
        RightLegSize = rightLegSize;
    }

    public Guid AgentId { get; }
    public int Depth { get; }
    public long TraversalOrder { get; }
    public bool LeftOccupied { get; }
    public bool RightOccupied { get; }
    public int LeftLegSize { get; }
    public int RightLegSize { get; }
    public bool HasAvailableSlot => !LeftOccupied || !RightOccupied;
}
