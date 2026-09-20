using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Domain.Notifications;

public sealed class Notification : OrganizationEntity
{
    private Notification() { }

    public Guid RecipientUserId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public NotificationContent Content { get; private set; } = null!;
    public string TemplateKey { get; private set; } = string.Empty;
    public string Culture { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public NotificationDeliveryStatus DeliveryStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? FailureCode { get; private set; }

    public static Notification Create(
        Guid organizationId,
        Guid recipientUserId,
        NotificationKind kind,
        NotificationContent content,
        string templateKey,
        string culture,
        string idempotencyKey,
        DateTimeOffset createdAt
    )
    {
        if (organizationId == Guid.Empty)
            throw new DomainInvariantException("Organization is required.");
        if (recipientUserId == Guid.Empty)
            throw new DomainInvariantException("Notification recipient is required.");
        if (!Enum.IsDefined(kind))
            throw new DomainInvariantException("Notification kind is invalid.");
        if (content is null)
            throw new DomainInvariantException("Notification content is required.");
        if (string.IsNullOrWhiteSpace(templateKey))
            throw new DomainInvariantException("Notification template is required.");
        if (string.IsNullOrWhiteSpace(culture))
            throw new DomainInvariantException("Notification culture is required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainInvariantException("Notification idempotency key is required.");
        if (createdAt == default)
            throw new DomainInvariantException("Notification creation time is required.");

        var notification = new Notification
        {
            OrganizationId = organizationId,
            RecipientUserId = recipientUserId,
            Kind = kind,
            Content = content,
            TemplateKey = templateKey.Trim().ToLowerInvariant(),
            Culture = culture.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            DeliveryStatus = NotificationDeliveryStatus.Pending,
            CreatedAt = createdAt,
        };
        notification.AddDomainEvent(
            new NotificationCreatedEvent(
                notification.OrganizationId,
                notification.Id,
                notification.RecipientUserId,
                notification.Kind,
                notification.CreatedAt
            )
        );
        return notification;
    }

    public bool MarkDelivered(DateTimeOffset deliveredAt)
    {
        if (DeliveryStatus == NotificationDeliveryStatus.Delivered)
            return false;
        if (deliveredAt < CreatedAt)
            throw new DomainInvariantException("Delivery time cannot precede creation time.");

        DeliveryStatus = NotificationDeliveryStatus.Delivered;
        DeliveredAt = deliveredAt;
        FailureCode = null;
        return true;
    }

    public bool MarkDeliveryFailed(
        string failureCode,
        int attemptCount,
        bool isDeadLettered,
        DateTimeOffset occurredAt
    )
    {
        if (string.IsNullOrWhiteSpace(failureCode) || attemptCount <= 0)
            throw new DomainInvariantException("Valid delivery failure details are required.");
        if (DeliveryStatus == NotificationDeliveryStatus.Delivered)
            throw new DomainInvariantException("A delivered notification cannot be failed.");

        var normalizedCode = failureCode.Trim().ToUpperInvariant();
        var targetStatus = isDeadLettered
            ? NotificationDeliveryStatus.DeadLettered
            : NotificationDeliveryStatus.Failed;
        if (DeliveryStatus == targetStatus && FailureCode == normalizedCode)
            return false;

        DeliveryStatus = targetStatus;
        FailureCode = normalizedCode;
        AddDomainEvent(
            new NotificationDeliveryFailedEvent(
                OrganizationId,
                Id,
                RecipientUserId,
                normalizedCode,
                attemptCount,
                isDeadLettered,
                occurredAt
            )
        );
        return true;
    }

    public bool MarkRead(DateTimeOffset readAt)
    {
        if (ReadAt.HasValue)
            return false;
        if (readAt < CreatedAt)
            throw new DomainInvariantException("Read time cannot precede creation time.");

        ReadAt = readAt;
        AddDomainEvent(new NotificationReadEvent(OrganizationId, Id, RecipientUserId, readAt));
        return true;
    }
}
