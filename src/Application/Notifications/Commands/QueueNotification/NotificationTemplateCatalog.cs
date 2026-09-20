using System.Globalization;
using modular_mlm.Domain.Notifications;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.Notifications.Commands.QueueNotification;

public static class NotificationTemplateCatalog
{
    public const string CommissionReleased = "commission-released";
    public const string PayoutRequested = "payout-requested";
    public const string PayoutApproved = "payout-approved";
    public const string PayoutCompleted = "payout-completed";
    public const string PayoutFailed = "payout-failed";
    public const string PayoutFailedOperations = "payout-failed-operations";
    public const string OrderPaid = "order-paid";
    public const string RefundCompleted = "refund-completed";
    public const string OutboxDeadLetter = "outbox-dead-letter";

    private static readonly HashSet<string> Supported =
    [
        CommissionReleased,
        PayoutRequested,
        PayoutApproved,
        PayoutCompleted,
        PayoutFailed,
        PayoutFailedOperations,
        OrderPaid,
        RefundCompleted,
        OutboxDeadLetter,
    ];

    public static bool IsSupported(string templateKey) =>
        !string.IsNullOrWhiteSpace(templateKey)
        && Supported.Contains(templateKey.Trim().ToLowerInvariant());

    public static NotificationContent Render(
        string templateKey,
        IReadOnlyDictionary<string, string> variables,
        string? actionPath
    )
    {
        var key = templateKey.Trim().ToLowerInvariant();
        return key switch
        {
            CommissionReleased => new NotificationContent(
                "Commission available",
                $"Your {Money(variables)} commission is now available.",
                actionPath
            ),
            PayoutRequested => new NotificationContent(
                "Payout requires review",
                $"A payout request for {Money(variables)} is awaiting review.",
                actionPath
            ),
            PayoutApproved => new NotificationContent(
                "Payout approved",
                $"Your payout request for {Money(variables)} was approved.",
                actionPath
            ),
            PayoutCompleted => new NotificationContent(
                "Payout completed",
                $"Your payout of {Money(variables)} was completed.",
                actionPath
            ),
            PayoutFailed => new NotificationContent(
                "Payout could not be completed",
                $"Your payout of {Money(variables)} could not be completed. Please review your payout account or contact support.",
                actionPath
            ),
            PayoutFailedOperations => new NotificationContent(
                "Payout processing alert",
                $"Payout {Required(variables, "payoutId")} failed with category {Required(variables, "failureCode")}.",
                actionPath
            ),
            OrderPaid => new NotificationContent(
                "Payment received",
                $"Payment for order {Required(variables, "orderNumber")} was received.",
                actionPath
            ),
            RefundCompleted => new NotificationContent(
                "Refund completed",
                $"Your refund of {Money(variables)} was completed.",
                actionPath
            ),
            OutboxDeadLetter => new NotificationContent(
                "Message processing requires attention",
                $"Message {Required(variables, "messageId")} could not be processed.",
                actionPath
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(templateKey),
                templateKey,
                "Notification template is not supported."
            ),
        };
    }

    private static string Money(IReadOnlyDictionary<string, string> variables)
    {
        var amountText = Required(variables, "amount");
        if (
            !decimal.TryParse(
                amountText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount
            )
        )
            throw new ArgumentException("Notification amount is invalid.", nameof(variables));
        var currency = Required(variables, "currency").ToUpperInvariant();
        return $"{currency} {amount:N2}";
    }

    private static string Required(IReadOnlyDictionary<string, string> variables, string name) =>
        variables.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException(
                $"Notification variable '{name}' is required.",
                nameof(variables)
            );
}
