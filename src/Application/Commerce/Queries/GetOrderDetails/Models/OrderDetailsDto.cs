using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetOrderDetails.Models;

public sealed record OrderDetailsDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    AddressDto ShippingAddress,
    AddressDto BillingAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? DeliveredAt,
    bool CanRequestCancellation,
    string? CancellationFailureReason,
    IReadOnlyList<OrderItemDetailsDto> Items
);
