using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAdminAgents.Models;

public sealed record AdminAgentSummaryDto(
    Guid AgentId,
    string AgentCode,
    string DisplayName,
    string Email,
    AgentStatus Status,
    Guid? SponsorAgentId,
    string? SponsorAgentCode,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ActivatedAt,
    bool IsPlaced,
    Guid? PlacementParentAgentId,
    PlacementSide? PlacementSide,
    string QualificationState
);
