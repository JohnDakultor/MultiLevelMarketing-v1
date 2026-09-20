namespace modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary.Models;

public sealed record AgentEarningsSummaryDto(
    Guid AgentId,
    decimal PendingAmount,
    decimal AvailableAmount,
    decimal PaidAmount,
    decimal DirectSalesLifetime,
    decimal BinaryPairingLifetime
);
