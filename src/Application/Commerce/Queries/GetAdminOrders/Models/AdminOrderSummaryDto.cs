using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetAdminOrders.Models;

public sealed record AdminOrderSummaryDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerDisplayName,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string Currency,
    decimal GrandTotal,
    int ItemCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? DeliveredAt
);
