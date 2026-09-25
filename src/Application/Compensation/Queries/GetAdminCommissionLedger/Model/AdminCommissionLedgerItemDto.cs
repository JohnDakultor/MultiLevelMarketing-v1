using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger.Model;

public sealed record AdminCommissionLedgerItemDto(
    Guid Id,
    Guid BeneficiaryAgentId,
    string AgentCode,
    Guid? SourceOrderId,
    Guid? SourceOrderItemId,
    Guid? SourceAgentId,
    Guid? PairingRunId,
    Guid CommissionPlanVersionId,
    string RuleId,
    CommissionType Type,
    decimal BaseAmount,
    decimal? Rate,
    decimal Amount,
    CommissionStatus Status,
    DateTimeOffset? AvailableAt,
    Guid? ReversalOfCommissionId,
    Guid? SourceOrderItemRefundId,
    DateTimeOffset CreatedAt
);
