namespace modular_mlm.Domain.Events;

public sealed record NotificationDeliveryFailedEvent(
    Guid OrganizationId,
    Guid NotificationId,
    Guid RecipientUserId,
    string FailureCode,
    int AttemptCount,
    bool IsDeadLettered,
    DateTimeOffset OccurredAt
) : BaseEvent;
