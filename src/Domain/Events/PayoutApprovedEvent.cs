namespace modular_mlm.Domain.Events;

public sealed record PayoutApprovedEvent(
    Guid OrganizationId,
    Guid PayoutRequestId,
    Guid AgentId,
    decimal Amount,
    string Currency,
    DateTimeOffset ApprovedAt
) : BaseEvent;
