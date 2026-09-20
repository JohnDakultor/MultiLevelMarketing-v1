using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails.Models;

public sealed record AttributedOrderDetailsDto(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? DeliveredAt,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string Currency,
    decimal GrandTotal,
    string MaskedCustomerName,
    IReadOnlyList<AttributedOrderItemDto> Items,
    IReadOnlyList<AgentCommissionSummaryDto> Commissions
);
