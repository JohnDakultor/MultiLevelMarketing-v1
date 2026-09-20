using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetMyOrders.Models;

public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string Currency,
    decimal GrandTotal,
    int ItemCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? DeliveredAt
);
