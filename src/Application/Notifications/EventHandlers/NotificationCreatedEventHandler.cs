using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.Events;

namespace modular_mlm.Application.Notifications.EventHandlers;

public sealed class NotificationCreatedEventHandler(
    IApplicationDbContext db,
    INotificationDeliveryOutbox deliveryOutbox
) : INotificationHandler<NotificationCreatedEvent>
{
    public async Task Handle(
        NotificationCreatedEvent notification,
        CancellationToken cancellationToken
    )
    {
        var exists = await db.Notifications.AnyAsync(
            candidate =>
                candidate.Id == notification.NotificationId
                && candidate.OrganizationId == notification.OrganizationId
                && candidate.RecipientUserId == notification.RecipientUserId
                && candidate.Kind == notification.Kind,
            cancellationToken
        );
        if (!exists)
            throw new InvalidOperationException(
                "Notification-created event does not match a persisted notification."
            );

        await deliveryOutbox.ActivateAsync(
            notification.NotificationId,
            NotificationChannel.Email,
            notification.OccurredAt,
            cancellationToken
        );
    }
}
