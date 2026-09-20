using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.Notifications;

namespace modular_mlm.Application.Notifications.Commands.QueueNotification;

public sealed class QueueNotificationCommandHandler(
    IApplicationDbContext db,
    INotificationRecipientResolver recipients,
    INotificationDeliveryOutbox deliveryOutbox,
    TimeProvider clock
) : IRequestHandler<QueueNotificationCommand, Guid>
{
    public async Task<Guid> Handle(
        QueueNotificationCommand request,
        CancellationToken cancellationToken
    )
    {
        var recipient = await recipients.FindAsync(
            request.OrganizationId,
            request.RecipientUserId.ToString(),
            cancellationToken
        );
        if (recipient is null)
            throw new KeyNotFoundException("Notification recipient was not found.");
        if (!recipient.EmailConfirmed || !recipient.Allows(request.Kind.ToString()))
            throw new InvalidOperationException(
                "The recipient is not eligible for this notification."
            );

        var content = NotificationTemplateCatalog.Render(
            request.TemplateKey,
            request.Variables,
            request.ActionPath
        );
        var normalizedKey = request.IdempotencyKey.Trim();
        var normalizedTemplate = request.TemplateKey.Trim().ToLowerInvariant();
        var existing = await db.Notifications.SingleOrDefaultAsync(
            notification =>
                notification.OrganizationId == request.OrganizationId
                && notification.IdempotencyKey == normalizedKey,
            cancellationToken
        );
        if (existing is not null)
        {
            if (
                existing.RecipientUserId != request.RecipientUserId
                || existing.Kind != request.Kind
                || existing.TemplateKey != normalizedTemplate
                || existing.Culture != request.Culture.Trim()
                || existing.Content != content
            )
                throw new IdempotencyConflictException(
                    "The notification idempotency key is already associated with a different request."
                );
            return existing.Id;
        }

        var now = clock.GetUtcNow();
        var notification = Notification.Create(
            request.OrganizationId,
            request.RecipientUserId,
            request.Kind,
            content,
            normalizedTemplate,
            request.Culture,
            normalizedKey,
            now
        );
        db.Notifications.Add(notification);
        await deliveryOutbox.StageAsync(
            new NotificationDeliveryRequest(
                notification.Id,
                notification.OrganizationId,
                recipient.UserId,
                recipient.Email,
                NotificationChannel.Email,
                normalizedTemplate,
                request.Culture,
                request.Variables
            ),
            now,
            cancellationToken
        );
        await db.SaveChangesAsync(cancellationToken);
        return notification.Id;
    }
}
