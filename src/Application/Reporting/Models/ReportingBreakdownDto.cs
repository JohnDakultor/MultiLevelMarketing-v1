using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Reporting.Models;

public sealed record SalesBreakdownDto(Guid Id, string Label, int Orders, decimal GrossSales);

public sealed record StatusAmountDto(string Status, int Count, decimal Amount);

public sealed record CommissionTypeAmountDto(CommissionType Type, int Count, decimal Amount);
