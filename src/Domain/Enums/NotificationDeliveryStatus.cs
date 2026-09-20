namespace modular_mlm.Domain.Notifications;

public enum NotificationDeliveryStatus
{
    Pending = 1,
    Delivered = 2,
    Failed = 3,
    DeadLettered = 4,
}
