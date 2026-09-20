namespace modular_mlm.Domain.Events;

public sealed record PayoutCompletedEvent(
    Guid OrganizationId,
    Guid PayoutRequestId,
    Guid AgentId,
    decimal Amount,
    string Currency,
    string ProviderReference,
    DateTimeOffset ProcessedAt
) : BaseEvent;
