using modular_mlm.Domain.Notifications;

namespace modular_mlm.Domain.Events;

public sealed record NotificationCreatedEvent(
    Guid OrganizationId,
    Guid NotificationId,
    Guid RecipientUserId,
    NotificationKind Kind,
    DateTimeOffset OccurredAt
) : BaseEvent;
