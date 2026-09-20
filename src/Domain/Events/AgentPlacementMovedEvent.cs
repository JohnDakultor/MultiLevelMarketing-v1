using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Events;

public sealed record AgentPlacementMovedEvent(
    Guid OrganizationId,
    Guid AgentId,
    Guid OldParentAgentId,
    PlacementSide OldSide,
    Guid NewParentAgentId,
    PlacementSide NewSide,
    string ActorUserId,
    string Reason,
    DateTimeOffset OccurredAt
) : BaseEvent;
