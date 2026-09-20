using modular_mlm.Domain.Commerce;

namespace modular_mlm.Domain.Services;

public sealed record OrderCancellationDecision(
    bool IsAllowed,
    string ReasonCode,
    string Explanation
);

public sealed class OrderCancellationPolicy
{
    public OrderCancellationDecision Evaluate(
        OrderStatus orderStatus,
        PaymentStatus paymentStatus,
        IEnumerable<FulfillmentStatus> fulfillmentStatuses
    )
    {
        if (orderStatus == OrderStatus.Cancelled)
            return Denied("already_cancelled", "The order is already cancelled.");

        if (orderStatus is OrderStatus.Refunded or OrderStatus.PartiallyRefunded)
            return Denied("already_refunded", "A refunded order cannot be cancelled.");

        if (fulfillmentStatuses.Any(status => status != FulfillmentStatus.Unfulfilled))
            return Denied("already_fulfilled", "Order fulfillment has already started.");

        if (
            paymentStatus
            is PaymentStatus.Authorized
                or PaymentStatus.Paid
                or PaymentStatus.PartiallyRefunded
                or PaymentStatus.Refunded
        )
            return Denied(
                "payment_action_required",
                "This order requires payment reversal or refund processing."
            );

        return orderStatus == OrderStatus.PendingPayment
            ? new OrderCancellationDecision(true, "allowed", "The order may be cancelled.")
            : Denied("status_not_cancellable", "The order can no longer be cancelled.");
    }

    private static OrderCancellationDecision Denied(string reasonCode, string explanation) =>
        new(false, reasonCode, explanation);
}
