using modular_mlm.Domain.Payouts;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutHistory.Models;

public sealed record PayoutHistoryItemDto(
    Guid Id,
    Guid AgentId,
    decimal Amount,
    string Currency,
    PayoutStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ProcessedAt,
    string? ProviderReference
);
