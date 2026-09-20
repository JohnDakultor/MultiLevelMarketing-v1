using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Events;

public sealed record AgentPreferredLegChangedEvent(
    Guid OrganizationId,
    Guid AgentId,
    PlacementSide PreferredLeg,
    DateTimeOffset OccurredAt
) : BaseEvent;
