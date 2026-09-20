using modular_mlm.Domain.Payouts;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutDetails.Models;

public sealed record PayoutDetailsDto(
    Guid Id,
    Guid AgentId,
    Guid PayoutAccountId,
    decimal Amount,
    string Currency,
    PayoutStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? ProcessedAt,
    string? ProviderBatchId,
    string? ProviderTransferId,
    string? ProviderReference,
    string? FailureCode,
    string? FailureMessage
);
