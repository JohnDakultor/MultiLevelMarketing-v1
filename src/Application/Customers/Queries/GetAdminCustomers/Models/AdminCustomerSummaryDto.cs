using modular_mlm.Domain.Identity;

namespace modular_mlm.Application.Customers.Queries.GetAdminCustomers.Models;

public sealed record AdminCustomerSummaryDto(
    Guid Id,
    string DisplayName,
    string Email,
    CustomerStatus Status,
    DateTimeOffset RegisteredAt,
    int OrderCount,
    decimal GrossOrderValue,
    string? Currency
);
