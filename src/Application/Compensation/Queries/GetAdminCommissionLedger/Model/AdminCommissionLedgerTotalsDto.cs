namespace modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger.Model;

public sealed record AdminCommissionLedgerTotalsDto(
    decimal NetAmount,
    decimal PendingAmount,
    decimal AvailableAmount,
    decimal HeldAmount,
    decimal PaidAmount,
    decimal ReversalAmount
);
