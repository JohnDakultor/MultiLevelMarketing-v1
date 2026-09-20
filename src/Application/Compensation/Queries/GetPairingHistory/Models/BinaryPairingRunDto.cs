using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Queries.GetPairingHistory.Models;

public sealed record BinaryPairingRunDto(
    Guid OrganizationId,
    Guid AgentId,
    Guid CommissionPlanId,
    int CommissionPlanVersion,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    bool QualificationPassed,
    string? QualificationFailureReason,
    decimal LeftBefore,
    decimal RightBefore,
    decimal MatchedVolume,
    decimal LeftConsumed,
    decimal RightConsumed,
    decimal LeftAfter,
    decimal RightAfter,
    decimal GrossCommission,
    decimal CappedAmount,
    decimal NetCommission,
    bool CapApplied,
    BinaryPairingRunStatus Status,
    DateTimeOffset? ProcessedAt
);
