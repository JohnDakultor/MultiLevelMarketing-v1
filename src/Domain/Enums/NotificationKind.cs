namespace modular_mlm.Domain.Notifications;

public enum NotificationKind
{
    CommissionReleased = 1,
    PayoutRequested = 2,
    PayoutApproved = 3,
    PayoutCompleted = 4,
    PayoutFailed = 5,
    AdministratorInvitation = 6,
    OrderPaid = 7,
    RefundCompleted = 8,
    OperationsAlert = 9,
}
