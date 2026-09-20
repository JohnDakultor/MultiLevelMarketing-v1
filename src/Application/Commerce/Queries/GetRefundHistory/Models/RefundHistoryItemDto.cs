using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Queries.GetRefundHistory.Models;

public sealed record RefundHistoryItemDto(
    Guid Id,
    Guid OrderId,
    Guid OrderItemId,
    decimal Quantity,
    decimal Amount,
    string Currency,
    string Reason,
    OrderItemRefundStatus ReversalStatus,
    PaymentRefundStatus ProviderStatus,
    string? ProviderRefundId,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ProviderCompletedAt,
    DateTimeOffset? ReversalCompletedAt,
    string? FailureSummary
);
