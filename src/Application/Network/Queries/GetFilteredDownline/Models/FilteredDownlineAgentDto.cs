using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetFilteredDownline.Models;

public sealed record FilteredDownlineAgentDto(
    Guid AgentId,
    string AgentCode,
    AgentStatus Status,
    Guid? SponsorAgentId,
    Guid? PlacementParentAgentId,
    PlacementSide? PlacementSide,
    int Depth,
    PlacementSide FirstLeg,
    string QualificationState,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ActivatedAt
);
