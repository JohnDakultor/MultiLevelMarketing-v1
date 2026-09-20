namespace modular_mlm.Domain.Events;

public sealed record OrganizationProvisionedEvent(
    Guid OrganizationId,
    string Slug,
    DateTimeOffset OccurredAt
) : BaseEvent;
