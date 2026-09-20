using System.Globalization;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Notifications.Commands.QueueNotification;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Notifications;

namespace modular_mlm.Application.Notifications.EventHandlers;

public sealed class PayoutRequestedEventHandler(
    IApplicationDbContext db,
    INotificationRecipientResolver recipients,
    ISender sender
) : INotificationHandler<PayoutRequestedEvent>
{
    public async Task Handle(PayoutRequestedEvent notification, CancellationToken cancellationToken)
    {
        var payout = await db
            .PayoutRequests.AsNoTracking()
            .Where(candidate =>
                candidate.Id == notification.PayoutRequestId
                && candidate.OrganizationId == notification.OrganizationId
            )
            .Select(candidate => new
            {
                candidate.OrganizationId,
                candidate.Amount,
                candidate.Currency,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (payout is null)
            throw new KeyNotFoundException("Payout request was not found.");

        var administrators = await recipients.GetOrganizationAdministratorsAsync(
            payout.OrganizationId,
            cancellationToken
        );
        foreach (
            var administrator in administrators.Where(recipient =>
                recipient.EmailConfirmed
                && recipient.Allows(NotificationKind.PayoutRequested.ToString())
            )
        )
        {
            if (!Guid.TryParse(administrator.UserId, out var recipientUserId))
                throw new InvalidOperationException("Administrator identity is invalid.");
            await sender.Send(
                new QueueNotificationCommand(
                    payout.OrganizationId,
                    recipientUserId,
                    NotificationKind.PayoutRequested,
                    NotificationTemplateCatalog.PayoutRequested,
                    administrator.Culture,
                    MoneyVariables(payout.Amount, payout.Currency),
                    "/admin/payouts",
                    $"payout-requested:{notification.PayoutRequestId:N}:{recipientUserId:N}"
                ),
                cancellationToken
            );
        }
    }

    private static IReadOnlyDictionary<string, string> MoneyVariables(
        decimal amount,
        string currency
    ) =>
        new Dictionary<string, string>
        {
            ["amount"] = amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["currency"] = currency,
        };
}
