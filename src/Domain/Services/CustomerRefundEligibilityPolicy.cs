using modular_mlm.Domain.Commerce;

namespace modular_mlm.Domain.Services;

public sealed record CustomerRefundEligibilityInput(
    OrderStatus OrderStatus,
    PaymentStatus PaymentStatus,
    FulfillmentStatus FulfillmentStatus,
    int PurchasedQuantity,
    decimal AllocatedRefundQuantity,
    bool HasPendingRefund,
    DateTimeOffset? DeliveredAt,
    int ReturnWindowDays,
    DateTimeOffset CurrentTime
);

public sealed record CustomerRefundEligibilityDecision(
    bool IsEligible,
    decimal MaximumQuantity,
    DateTimeOffset? EligibleUntil,
    string ReasonCode,
    string Explanation
);

public sealed class CustomerRefundEligibilityPolicy
{
    public CustomerRefundEligibilityDecision Evaluate(CustomerRefundEligibilityInput input)
    {
        var remainingQuantity = Math.Max(
            0m,
            input.PurchasedQuantity - input.AllocatedRefundQuantity
        );

        if (remainingQuantity == 0m)
            return Denied("fully_allocated", "The complete item quantity is already refunded.");

        if (input.HasPendingRefund)
            return Denied(
                "refund_pending",
                "A refund request for this item is already pending.",
                remainingQuantity
            );

        if (input.PaymentStatus is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
            return Denied(
                "payment_not_refundable",
                "Only a paid order can be refunded.",
                remainingQuantity
            );

        if (input.OrderStatus != OrderStatus.Delivered || input.DeliveredAt is null)
            return Denied(
                "order_not_delivered",
                "A customer refund can be requested after delivery.",
                remainingQuantity
            );

        if (input.FulfillmentStatus is FulfillmentStatus.Cancelled or FulfillmentStatus.Refunded)
            return Denied(
                "item_not_refundable",
                "The item is already cancelled or refunded.",
                remainingQuantity
            );

        if (input.ReturnWindowDays <= 0)
            return Denied(
                "return_window_disabled",
                "Customer returns are not enabled for this organization.",
                remainingQuantity
            );

        var eligibleUntil = input.DeliveredAt.Value.AddDays(input.ReturnWindowDays);
        if (input.CurrentTime > eligibleUntil)
            return Denied(
                "return_window_elapsed",
                "The return window for this item has elapsed.",
                remainingQuantity,
                eligibleUntil
            );

        return new CustomerRefundEligibilityDecision(
            true,
            remainingQuantity,
            eligibleUntil,
            "allowed",
            "The item is eligible for a refund request."
        );
    }

    private static CustomerRefundEligibilityDecision Denied(
        string reasonCode,
        string explanation,
        decimal maximumQuantity = 0m,
        DateTimeOffset? eligibleUntil = null
    ) => new(false, maximumQuantity, eligibleUntil, reasonCode, explanation);
}
