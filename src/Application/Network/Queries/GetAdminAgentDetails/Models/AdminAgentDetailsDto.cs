using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAdminAgentDetails.Models;

public sealed record AdminAgentDetailsDto(
    Guid AgentId,
    string AgentCode,
    string ReferralCode,
    string DisplayName,
    string Email,
    bool EmailConfirmed,
    AgentStatus Status,
    Guid? SponsorAgentId,
    string? SponsorAgentCode,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ActivatedAt,
    string QualificationState,
    AgentPlacementStatusDto Placement,
    bool CanApprove,
    bool CanActivate,
    bool CanSuspend,
    bool CanReactivate
);
