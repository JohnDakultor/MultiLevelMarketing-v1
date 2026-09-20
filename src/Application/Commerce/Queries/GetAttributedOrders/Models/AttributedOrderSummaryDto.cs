using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrders.Models;

public sealed record AttributedOrderSummaryDto(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string Currency,
    decimal GrandTotal,
    decimal CommissionableAmount,
    decimal BusinessVolume,
    int ItemCount,
    IReadOnlyList<string> ProductNames,
    string MaskedCustomerName,
    decimal CommissionAmount
);
