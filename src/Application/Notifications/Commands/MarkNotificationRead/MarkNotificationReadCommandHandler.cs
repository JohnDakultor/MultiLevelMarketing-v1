using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(
    IApplicationDbContext db,
    IUser currentUser,
    TimeProvider clock
) : IRequestHandler<MarkNotificationReadCommand, bool>
{
    public async Task<bool> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken
    )
    {
        if (currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var notification = await db.Notifications.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.NotificationId
                && candidate.OrganizationId == request.OrganizationId
                && candidate.RecipientUserId == currentUser.UserId,
            cancellationToken
        );
        if (notification is null)
            throw new KeyNotFoundException("Notification was not found.");

        if (!notification.MarkRead(clock.GetUtcNow()))
            return false;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
