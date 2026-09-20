using MediatR;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Notifications.Commands.QueueNotification;
using modular_mlm.Domain.Notifications;
using modular_mlm.Infrastructure.Notifications;

namespace modular_mlm.Infrastructure.Messaging;

public sealed class OutboxDeadLetterAlertSender(
    INotificationRecipientResolver recipients,
    ISender sender
)
{
    public async Task SendAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (message.OrganizationId is not { } organizationId)
            return;
        await SendAsync(organizationId, message.Id, message.MessageType, cancellationToken);
    }

    public async Task SendAsync(
        Guid organizationId,
        Guid messageId,
        string messageType,
        CancellationToken cancellationToken
    )
    {
        var administrators = await recipients.GetOrganizationAdministratorsAsync(
            organizationId,
            cancellationToken
        );
        foreach (
            var administrator in administrators.Where(recipient =>
                recipient.EmailConfirmed
                && recipient.Allows(NotificationKind.OperationsAlert.ToString())
            )
        )
        {
            if (!Guid.TryParse(administrator.UserId, out var userId))
                continue;
            await sender.Send(
                new QueueNotificationCommand(
                    organizationId,
                    userId,
                    NotificationKind.OperationsAlert,
                    NotificationTemplateCatalog.OutboxDeadLetter,
                    administrator.Culture,
                    new Dictionary<string, string>
                    {
                        ["messageId"] = messageId.ToString("D"),
                        ["messageType"] = messageType,
                    },
                    "/admin/messaging/dead-letters",
                    $"operations-dead-letter:{messageId:N}:{userId:N}"
                ),
                cancellationToken
            );
        }
    }
}
