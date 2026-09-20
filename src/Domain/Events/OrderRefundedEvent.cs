namespace modular_mlm.Domain.Events;

public sealed record OrderRefundedEvent(Guid OrderId) : BaseEvent;
