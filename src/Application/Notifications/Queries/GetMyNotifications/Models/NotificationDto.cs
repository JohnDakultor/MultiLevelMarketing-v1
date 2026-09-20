using modular_mlm.Domain.Notifications;

namespace modular_mlm.Application.Notifications.Queries.GetMyNotifications.Models;

public sealed record NotificationDto(
    Guid Id,
    NotificationKind Kind,
    string Title,
    string PlainTextBody,
    string? ActionPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? ReadAt,
    bool IsRead
);
