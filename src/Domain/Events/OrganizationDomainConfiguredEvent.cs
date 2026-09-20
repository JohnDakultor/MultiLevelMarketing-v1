namespace modular_mlm.Domain.Events;

public sealed record OrganizationDomainConfiguredEvent(
    Guid OrganizationId,
    Guid OrganizationDomainId,
    string HostName,
    bool IsPrimary,
    bool IsVerified,
    DateTimeOffset OccurredAt
) : BaseEvent;
