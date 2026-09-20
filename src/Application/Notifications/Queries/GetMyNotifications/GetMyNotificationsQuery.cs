using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Notifications.Queries.GetMyNotifications.Models;
using modular_mlm.Domain.Notifications;

namespace modular_mlm.Application.Notifications.Queries.GetMyNotifications;

[Authorize]
public sealed record GetMyNotificationsQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    bool UnreadOnly = false,
    NotificationKind? Kind = null
) : IRequest<NotificationPageDto>, IOrganizationMemberRequest;
