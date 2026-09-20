using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetDirectPlacementChildren.Models;

public sealed record PlacementChildDto(
    Guid AgentId,
    string AgentCode,
    AgentStatus Status,
    PlacementSide Side,
    DateTimeOffset JoinedAt
);
