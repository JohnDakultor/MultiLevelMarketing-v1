using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetDirectRecruits.Models;

public sealed record DirectRecruitDto(
    Guid AgentId,
    string AgentCode,
    string ReferralCode,
    AgentStatus Status,
    Guid? PlacementParentAgentId,
    PlacementSide? PlacementSide,
    DateTimeOffset JoinedAt
);
