namespace modular_mlm.Domain.Events;

public sealed record CommissionCreatedEvent(Guid CommissionId) : BaseEvent;
