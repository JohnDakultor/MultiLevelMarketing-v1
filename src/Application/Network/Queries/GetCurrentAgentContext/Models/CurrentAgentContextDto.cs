using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetCurrentAgentContext.Models;

public sealed record CurrentAgentContextDto(
    Guid AgentId,
    string AgentCode,
    AgentStatus Status,
    bool IsPlaced,
    bool CanChoosePreferredLeg,
    bool CanShareReferralLinks,
    bool CanRequestPayout,
    string QualificationState
);
