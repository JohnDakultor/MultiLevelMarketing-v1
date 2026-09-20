namespace modular_mlm.Application.Reporting.Models;

public sealed record AgentReportDto(
    Guid OrganizationId,
    Guid AgentId,
    DateTimeOffset From,
    DateTimeOffset To,
    string Currency,
    int AttributedOrders,
    decimal GrossAttributedSales,
    decimal RefundedAttributedSales,
    decimal NetAttributedSales,
    int DirectRecruits,
    int DownlineSize,
    decimal BusinessVolume,
    IReadOnlyList<CommissionTypeAmountDto> Commissions,
    decimal CommissionTotal,
    IReadOnlyList<StatusAmountDto> Payouts
);
