namespace modular_mlm.Domain.Events;

public sealed record PayoutFailedEvent(
    Guid OrganizationId,
    Guid PayoutRequestId,
    Guid AgentId,
    decimal Amount,
    string Currency,
    string? ProviderReference,
    string? FailureCode
) : BaseEvent;
