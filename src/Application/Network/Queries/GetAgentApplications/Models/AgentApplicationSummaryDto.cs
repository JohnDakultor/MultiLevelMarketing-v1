using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAgentApplications.Models;

public sealed record AgentApplicationSummaryDto(
    Guid AgentId,
    string DisplayName,
    string Email,
    string AgentCode,
    AgentStatus Status,
    Guid? SponsorAgentId,
    string? SponsorAgentCode,
    DateTimeOffset JoinedAt,
    bool IsPlaced
);
