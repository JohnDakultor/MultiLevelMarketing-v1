using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Notifications.Commands.MarkNotificationRead;

[Authorize]
public sealed record MarkNotificationReadCommand(Guid OrganizationId, Guid NotificationId)
    : IRequest<bool>,
        IOrganizationMemberRequest;
