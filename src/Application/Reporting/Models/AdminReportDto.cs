namespace modular_mlm.Application.Reporting.Models;

public sealed record AdminReportDto(
    Guid OrganizationId,
    DateTimeOffset From,
    DateTimeOffset To,
    string Currency,
    int OrderCount,
    decimal GrossSales,
    decimal RefundedAmount,
    decimal NetSales,
    IReadOnlyList<SalesBreakdownDto> SalesByProduct,
    IReadOnlyList<SalesBreakdownDto> SalesByAgent,
    int NewAgents,
    int ActiveAgents,
    decimal BusinessVolume,
    decimal CommissionExpense,
    decimal CommissionLiability,
    decimal WalletLiability,
    IReadOnlyList<StatusAmountDto> Payouts
);
