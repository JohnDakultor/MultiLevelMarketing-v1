using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Notifications.Commands.MarkNotificationRead;
using modular_mlm.Application.Notifications.Queries.GetMyNotifications;
using modular_mlm.Application.Notifications.Queries.GetMyNotifications.Models;
using modular_mlm.Domain.Notifications;

namespace modular_mlm.Web.Endpoints;

public sealed class Notifications : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/me/notifications";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
        group.MapGet(GetMyNotificationPage);
        group.MapPut(MarkMyNotificationRead, "{notificationId:guid}/read");
    }

    public static async Task<Ok<NotificationPageDto>> GetMyNotificationPage(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false,
        NotificationKind? kind = null
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetMyNotificationsQuery(organizationId, page, pageSize, unreadOnly, kind)
            )
        );

    public static async Task<NoContent> MarkMyNotificationRead(
        ISender sender,
        Guid organizationId,
        Guid notificationId
    )
    {
        await sender.Send(new MarkNotificationReadCommand(organizationId, notificationId));
        return TypedResults.NoContent();
    }
}
