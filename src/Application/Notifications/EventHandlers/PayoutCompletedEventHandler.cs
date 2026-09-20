using System.Globalization;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Notifications.Commands.QueueNotification;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Notifications;

namespace modular_mlm.Application.Notifications.EventHandlers;

public sealed class PayoutCompletedEventHandler(IApplicationDbContext db, ISender sender)
    : INotificationHandler<PayoutCompletedEvent>
{
    public async Task Handle(PayoutCompletedEvent notification, CancellationToken cancellationToken)
    {
        var userId = await ResolveAgentUserIdAsync(
            notification.OrganizationId,
            notification.PayoutRequestId,
            notification.AgentId,
            cancellationToken
        );
        await sender.Send(
            new QueueNotificationCommand(
                notification.OrganizationId,
                userId,
                NotificationKind.PayoutCompleted,
                NotificationTemplateCatalog.PayoutCompleted,
                "en-PH",
                new Dictionary<string, string>
                {
                    ["amount"] = notification.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    ["currency"] = notification.Currency,
                },
                "/agent/payouts",
                $"payout-completed:{notification.PayoutRequestId:N}"
            ),
            cancellationToken
        );
    }

    private async Task<Guid> ResolveAgentUserIdAsync(
        Guid organizationId,
        Guid payoutRequestId,
        Guid agentId,
        CancellationToken cancellationToken
    )
    {
        var userId = await (
            from payout in db.PayoutRequests.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on payout.AgentId equals agent.Id
            where
                payout.Id == payoutRequestId
                && payout.OrganizationId == organizationId
                && payout.AgentId == agentId
                && agent.OrganizationId == organizationId
            select agent.UserId
        ).SingleOrDefaultAsync(cancellationToken);
        return Guid.TryParse(userId, out var parsed)
            ? parsed
            : throw new InvalidOperationException("Payout recipient identity is invalid.");
    }
}
