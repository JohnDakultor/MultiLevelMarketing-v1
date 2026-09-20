namespace modular_mlm.Application.Network.Queries.GetAgentQualificationStatus.Models;

public sealed record AgentQualificationFailureDto(string Code, string Message);

public sealed record AgentQualificationStatusDto(
    bool IsQualified,
    string State,
    IReadOnlyList<AgentQualificationFailureDto> Failures,
    decimal PersonalSalesAmount,
    decimal PersonalBusinessVolume,
    int ActiveDirectRecruitCount,
    bool HasActiveLeftLeg,
    bool HasActiveRightLeg,
    Guid? CommissionPlanId,
    int? CommissionPlanVersion,
    DateTimeOffset? EffectiveAt,
    DateTimeOffset EvaluatedAt
);
