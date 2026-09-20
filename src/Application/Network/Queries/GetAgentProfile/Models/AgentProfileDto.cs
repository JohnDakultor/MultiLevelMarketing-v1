using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAgentProfile.Models;

public sealed record AgentProfileDto(
    Guid AgentId,
    string AgentCode,
    string ReferralCode,
    string DisplayName,
    string Email,
    AgentStatus Status,
    Guid? SponsorAgentId,
    string? SponsorAgentCode,
    Guid? PlacementParentAgentId,
    string? PlacementParentAgentCode,
    PlacementSide? PlacementSide,
    PlacementSide? PreferredLeg,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ActivatedAt,
    string QualificationState
);
