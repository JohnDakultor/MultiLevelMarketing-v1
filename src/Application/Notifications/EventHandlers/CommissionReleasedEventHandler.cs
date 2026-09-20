using System.Globalization;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Notifications.Commands.QueueNotification;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Notifications;

namespace modular_mlm.Application.Notifications.EventHandlers;

public sealed class CommissionReleasedEventHandler(IApplicationDbContext db, ISender sender)
    : INotificationHandler<CommissionReleasedEvent>
{
    public async Task Handle(
        CommissionReleasedEvent notification,
        CancellationToken cancellationToken
    )
    {
        var data = await (
            from commission in db.CommissionTransactions.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on commission.BeneficiaryAgentId equals agent.Id
            join organization in db.Organizations.AsNoTracking()
                on commission.OrganizationId equals organization.Id
            where
                commission.Id == notification.CommissionId
                && commission.OrganizationId == notification.OrganizationId
                && commission.BeneficiaryAgentId == notification.BeneficiaryAgentId
            select new
            {
                commission.Amount,
                agent.UserId,
                organization.CurrencyCode,
            }
        ).SingleOrDefaultAsync(cancellationToken);
        if (data is null || !Guid.TryParse(data.UserId, out var recipientUserId))
            throw new InvalidOperationException(
                "Released commission does not have a valid tenant-owned recipient."
            );

        await sender.Send(
            new QueueNotificationCommand(
                notification.OrganizationId,
                recipientUserId,
                NotificationKind.CommissionReleased,
                NotificationTemplateCatalog.CommissionReleased,
                "en-PH",
                new Dictionary<string, string>
                {
                    ["amount"] = data.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    ["currency"] = data.CurrencyCode,
                },
                "/agent/wallet",
                $"commission-released:{notification.CommissionId:N}"
            ),
            cancellationToken
        );
    }
}
