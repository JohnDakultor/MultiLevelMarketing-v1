using modular_mlm.Application.Commerce.Queries.GetOrderDetails.Models;
using modular_mlm.Application.Commerce.Queries.GetRefundHistory.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails.Models;

public sealed record AdminOrderDetailsDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerDisplayName,
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
    IReadOnlyList<AdminOrderItemDto> Items,
    IReadOnlyList<AdminOrderPaymentDto> Payments,
    IReadOnlyList<RefundHistoryItemDto> ItemRefunds
);

public sealed record AdminOrderItemDto(
    Guid Id,
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    decimal BusinessVolume,
    FulfillmentStatus FulfillmentStatus
);

public sealed record AdminOrderPaymentDto(
    Guid Id,
    string Provider,
    string? ProviderCheckoutSessionId,
    string? ProviderPaymentId,
    decimal Amount,
    decimal RefundedAmount,
    string Currency,
    PaymentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    string? FailureCode,
    string? FailureMessage,
    IReadOnlyList<AdminPaymentRefundDto> Refunds
);

public sealed record AdminPaymentRefundDto(
    Guid Id,
    decimal Amount,
    string Currency,
    string Reason,
    string? ProviderRefundId,
    PaymentRefundStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    string? FailureMessage
);
