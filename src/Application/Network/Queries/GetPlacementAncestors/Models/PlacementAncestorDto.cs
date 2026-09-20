using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetPlacementAncestors.Models;

public sealed record PlacementAncestorDto(
    Guid AgentId,
    string AgentCode,
    AgentStatus Status,
    int Depth,
    PlacementSide DescendantLeg
);
