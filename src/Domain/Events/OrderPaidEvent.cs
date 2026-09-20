namespace modular_mlm.Domain.Events;

public sealed record OrderPaidEvent(Guid OrganizationId, Guid OrderId) : BaseEvent;
