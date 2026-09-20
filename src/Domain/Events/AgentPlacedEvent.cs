using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Events;

public sealed record AgentPlacedEvent(Guid AgentId, Guid ParentId, PlacementSide Side) : BaseEvent;
