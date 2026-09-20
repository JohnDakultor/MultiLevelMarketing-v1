namespace modular_mlm.Application.Reporting.Models;

public sealed record DashboardTaskDto(string Code, string Label, int Count, string Severity);

public sealed record DashboardAlertDto(string Code, string Message, string Severity);

public sealed record AdminDashboardDto(
    Guid OrganizationId,
    DateTimeOffset ObservedAt,
    string Currency,
    decimal SalesToday,
    decimal SalesLast30Days,
    int OrdersToday,
    int ActiveAgents,
    decimal CommissionLiability,
    decimal WalletLiability,
    IReadOnlyList<DashboardTaskDto> Tasks,
    IReadOnlyList<DashboardAlertDto> Alerts
);
