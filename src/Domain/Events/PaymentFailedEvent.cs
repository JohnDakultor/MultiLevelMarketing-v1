namespace modular_mlm.Domain.Events;

public sealed record PaymentFailedEvent(Guid OrganizationId, Guid PaymentId) : BaseEvent;
