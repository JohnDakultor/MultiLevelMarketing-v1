using modular_mlm.Domain.Identity;

namespace modular_mlm.Application.Customers.Queries.GetAdminCustomerDetails.Models;

public sealed record AdminCustomerOrderSummaryDto(
    Guid Id,
    string OrderNumber,
    int Status,
    int PaymentStatus,
    decimal GrandTotal,
    string Currency,
    DateTimeOffset Created
);

public sealed record AdminCustomerDetailsDto(
    Guid Id,
    string DisplayName,
    string Email,
    CustomerStatus Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastModified,
    int AddressCount,
    int OrderCount,
    decimal GrossOrderValue,
    IReadOnlyList<AdminCustomerOrderSummaryDto> RecentOrders
);
