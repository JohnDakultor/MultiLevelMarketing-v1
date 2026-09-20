using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Notifications.Queries.GetMyNotifications.Models;

namespace modular_mlm.Application.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler(IApplicationDbContext db, IUser currentUser)
    : IRequestHandler<GetMyNotificationsQuery, NotificationPageDto>
{
    public async Task<NotificationPageDto> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken
    )
    {
        if (currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var query = db
            .Notifications.AsNoTracking()
            .Where(notification =>
                notification.OrganizationId == request.OrganizationId
                && notification.RecipientUserId == currentUser.UserId
            );
        if (request.UnreadOnly)
            query = query.Where(notification => notification.ReadAt == null);
        if (request.Kind.HasValue)
            query = query.Where(notification => notification.Kind == request.Kind.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var unreadCount = await db.Notifications.CountAsync(
            notification =>
                notification.OrganizationId == request.OrganizationId
                && notification.RecipientUserId == currentUser.UserId
                && notification.ReadAt == null,
            cancellationToken
        );
        var items = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Kind,
                notification.Content.Title,
                notification.Content.PlainTextBody,
                notification.Content.ActionPath,
                notification.CreatedAt,
                notification.DeliveredAt,
                notification.ReadAt,
                notification.ReadAt != null
            ))
            .ToListAsync(cancellationToken);

        return new NotificationPageDto(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            unreadCount
        );
    }
}
