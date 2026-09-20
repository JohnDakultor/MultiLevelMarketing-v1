namespace modular_mlm.Domain.Events;

public sealed record AgentActivatedEvent(Guid AgentId) : BaseEvent;
