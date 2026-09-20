using System.Globalization;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Notifications.Commands.QueueNotification;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Notifications;

namespace modular_mlm.Application.Notifications.EventHandlers;

public sealed class PayoutFailedEventHandler(
    IApplicationDbContext db,
    INotificationRecipientResolver recipients,
    ISender sender
) : INotificationHandler<PayoutFailedEvent>
{
    public async Task Handle(PayoutFailedEvent notification, CancellationToken cancellationToken)
    {
        var agentUserId = await (
            from payout in db.PayoutRequests.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on payout.AgentId equals agent.Id
            where
                payout.Id == notification.PayoutRequestId
                && payout.OrganizationId == notification.OrganizationId
                && payout.AgentId == notification.AgentId
                && agent.OrganizationId == notification.OrganizationId
            select agent.UserId
        ).SingleOrDefaultAsync(cancellationToken);
        if (!Guid.TryParse(agentUserId, out var recipientUserId))
            throw new InvalidOperationException("Payout recipient identity is invalid.");

        var money = new Dictionary<string, string>
        {
            ["amount"] = notification.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["currency"] = notification.Currency,
        };
        await sender.Send(
            new QueueNotificationCommand(
                notification.OrganizationId,
                recipientUserId,
                NotificationKind.PayoutFailed,
                NotificationTemplateCatalog.PayoutFailed,
                "en-PH",
                money,
                "/agent/payouts",
                $"payout-failed:{notification.PayoutRequestId:N}"
            ),
            cancellationToken
        );

        var safeCode = NormalizeFailureCode(notification.FailureCode);
        var administrators = await recipients.GetOrganizationAdministratorsAsync(
            notification.OrganizationId,
            cancellationToken
        );
        foreach (
            var administrator in administrators.Where(recipient =>
                recipient.EmailConfirmed
                && recipient.Allows(NotificationKind.OperationsAlert.ToString())
            )
        )
        {
            if (!Guid.TryParse(administrator.UserId, out var administratorId))
                throw new InvalidOperationException("Administrator identity is invalid.");
            await sender.Send(
                new QueueNotificationCommand(
                    notification.OrganizationId,
                    administratorId,
                    NotificationKind.OperationsAlert,
                    NotificationTemplateCatalog.PayoutFailedOperations,
                    administrator.Culture,
                    new Dictionary<string, string>
                    {
                        ["payoutId"] = notification.PayoutRequestId.ToString("D"),
                        ["failureCode"] = safeCode,
                    },
                    $"/admin/payouts/{notification.PayoutRequestId:D}",
                    $"payout-failed-ops:{notification.PayoutRequestId:N}:{administratorId:N}"
                ),
                cancellationToken
            );
        }
    }

    private static string NormalizeFailureCode(string? failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
            return "UNSPECIFIED_PROVIDER_FAILURE";
        var normalized = new string(
            failureCode
                .Where(character => char.IsAsciiLetterOrDigit(character) || character == '_')
                .Take(64)
                .ToArray()
        );
        return string.IsNullOrWhiteSpace(normalized)
            ? "UNSPECIFIED_PROVIDER_FAILURE"
            : normalized.ToUpperInvariant();
    }
}
