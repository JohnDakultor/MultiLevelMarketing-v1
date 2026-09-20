using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Notifications.EventHandlers;

public sealed class AdministratorInvitedEventHandler(
    IApplicationDbContext db,
    IInvitationDeliveryOutbox deliveryOutbox,
    TimeProvider clock
) : INotificationHandler<AdministratorInvitedEvent>
{
    public async Task Handle(
        AdministratorInvitedEvent notification,
        CancellationToken cancellationToken
    )
    {
        var invitation = await db
            .AdministratorInvitations.AsNoTracking()
            .Where(candidate =>
                candidate.Id == notification.InvitationId
                && candidate.OrganizationId == notification.OrganizationId
            )
            .Select(candidate => new { candidate.Status, candidate.ExpiresAt })
            .SingleOrDefaultAsync(cancellationToken);
        if (invitation is null)
            throw new KeyNotFoundException("Administrator invitation was not found.");
        if (
            invitation.Status != AdministratorInvitationStatus.Pending
            || invitation.ExpiresAt <= clock.GetUtcNow()
        )
            return;

        await deliveryOutbox.ActivateAsync(
            notification.OrganizationId,
            notification.InvitationId,
            clock.GetUtcNow(),
            cancellationToken
        );
    }
}
