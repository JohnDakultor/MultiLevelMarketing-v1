namespace modular_mlm.Domain.Events;

public sealed record PayoutRequestedEvent(Guid OrganizationId, Guid PayoutRequestId) : BaseEvent;
