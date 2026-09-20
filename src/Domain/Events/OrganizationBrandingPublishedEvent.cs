namespace modular_mlm.Domain.Events;

public sealed record OrganizationBrandingPublishedEvent(
    Guid OrganizationId,
    int BrandingRevision,
    DateTimeOffset PublishedAt
) : BaseEvent;
