using modular_mlm.Domain.Notifications;

namespace modular_mlm.Application.Notifications.Commands.QueueNotification;

public sealed record QueueNotificationCommand(
    Guid OrganizationId,
    Guid RecipientUserId,
    NotificationKind Kind,
    string TemplateKey,
    string Culture,
    IReadOnlyDictionary<string, string> Variables,
    string? ActionPath,
    string IdempotencyKey
) : IRequest<Guid>;
