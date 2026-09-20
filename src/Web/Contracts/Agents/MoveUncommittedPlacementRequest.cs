using modular_mlm.Domain.Network;

namespace modular_mlm.Web.Contracts.Agents;

public sealed record MoveUncommittedPlacementRequest(
    Guid NewParentAgentId,
    PlacementSide NewSide,
    Guid ExpectedCurrentParentAgentId,
    PlacementSide ExpectedCurrentSide,
    string Reason
);
