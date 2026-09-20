namespace modular_mlm.Domain.Events;

public sealed record NotificationReadEvent(
    Guid OrganizationId,
    Guid NotificationId,
    Guid RecipientUserId,
    DateTimeOffset ReadAt
) : BaseEvent;
