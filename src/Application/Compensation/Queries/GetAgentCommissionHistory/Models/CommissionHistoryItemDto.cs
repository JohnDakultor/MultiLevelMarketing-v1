using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory.Models;

public sealed record CommissionHistoryItemDto(
    Guid Id,
    Guid? SourceOrderId,
    Guid? SourceOrderItemId,
    Guid? PairingRunId,
    CommissionType Type,
    decimal BaseAmount,
    decimal? Rate,
    decimal Amount,
    CommissionStatus Status,
    DateTimeOffset Created
);
