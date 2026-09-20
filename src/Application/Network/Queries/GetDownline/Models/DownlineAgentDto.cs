using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetDownline.Models;

public sealed record DownlineAgentDto(
    Guid AgentId,
    string AgentCode,
    AgentStatus Status,
    Guid? PlacementParentAgentId,
    PlacementSide? PlacementSide,
    int Depth,
    PlacementSide FirstLeg,
    DateTimeOffset JoinedAt
);
