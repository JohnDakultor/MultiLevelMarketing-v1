using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAgentApplication.Models;

public sealed record AgentApplicationDto(
    Guid AgentId,
    string AgentCode,
    AgentStatus Status,
    Guid? SponsorAgentId,
    string? SponsorAgentCode,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ActivatedAt,
    string QualificationState,
    bool IsPlacementPending
);
