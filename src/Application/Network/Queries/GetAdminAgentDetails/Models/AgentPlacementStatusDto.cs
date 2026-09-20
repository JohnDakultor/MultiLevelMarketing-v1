using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAdminAgentDetails.Models;

public sealed record AgentPlacementStatusDto(
    bool IsPlaced,
    Guid? ParentAgentId,
    string? ParentAgentCode,
    PlacementSide? Side,
    PlacementSide? PreferredLeg,
    IReadOnlyList<Guid> DirectChildAgentIds,
    int DescendantCount,
    bool HasFinancialActivity,
    bool IsMoveEligible,
    string MoveEligibilityReasonCode,
    string MoveEligibilityReason
);
