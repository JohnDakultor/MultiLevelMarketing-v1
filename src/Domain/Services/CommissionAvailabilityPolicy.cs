
using Domain.Enums;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Domain.Services;

public sealed class CommissionAvailabilityPolicy
{
    public record CommissionAvailabilityInput
    {
        public CommissionStatus CommissionStatus { get; init; }
        public DateTimeOffset CommissionCreatedAt { get; init; }
        public bool IsOrderBacked { get; init; }
        public PaymentStatus? OrderPaymentStatus { get; init; }
        public DateTimeOffset? OrderPaidAt { get; init; }
        public OrderStatus? OrderStatus { get; init; }
        public DateTimeOffset? OrderDeliveredAt { get; init; }
        public bool IsReversedOrRefunded { get; init; }
        public WalletSettings WalletSettings { get; init; } = null!;
        public DateTimeOffset CurrentTime { get; init; }
    }

    public CommissionAvailabilityDecision Evaluate(CommissionAvailabilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.WalletSettings);

        DateTimeOffset eligibleAt;

        if (input.CommissionStatus != CommissionStatus.Pending)
            return CommissionAvailabilityDecision.Ineligible(
                CommissionAvailabilityDecision.ReasonCodes.CommissionNotPending,
                "Only pending commissions can become available."
            );

        if (input.IsReversedOrRefunded)
            return CommissionAvailabilityDecision.Ineligible(
                CommissionAvailabilityDecision.ReasonCodes.SourceReversed,
                "The source transaction was reversed or fully refunded."
            );

        switch (input.WalletSettings.CommissionReleaseTrigger)
        {
            case CommissionReleaseTrigger.PaymentConfirmed:
                if (!input.IsOrderBacked)
                {
                    eligibleAt = input.CommissionCreatedAt.AddDays(
                        input.WalletSettings.ReleaseDelayDays
                    );
                    break;
                }

                if (input.OrderPaymentStatus != PaymentStatus.Paid || input.OrderPaidAt == null)
                    return CommissionAvailabilityDecision.Pending(
                        CommissionAvailabilityDecision.ReasonCodes.PaymentNotConfirmed,
                        "Payment has not been confirmed."
                    );

                eligibleAt = input.OrderPaidAt.Value.AddDays(input.WalletSettings.ReleaseDelayDays);
                break;

            case CommissionReleaseTrigger.OrderDelivered:
                if (input.OrderStatus != OrderStatus.Delivered || input.OrderDeliveredAt == null)
                    return CommissionAvailabilityDecision.Pending(
                        CommissionAvailabilityDecision.ReasonCodes.OrderNotDelivered,
                        "The order has not been delivered."
                    );

                eligibleAt = input.OrderDeliveredAt.Value.AddDays(
                    input.WalletSettings.ReleaseDelayDays
                );
                break;

            case CommissionReleaseTrigger.ReturnWindowElapsed:
                if (input.OrderStatus != OrderStatus.Delivered || input.OrderDeliveredAt == null)
                    return CommissionAvailabilityDecision.Pending(
                        CommissionAvailabilityDecision.ReasonCodes.OrderNotDelivered,
                        "The order has not been delivered."
                    );

                eligibleAt = input
                    .OrderDeliveredAt.Value.AddDays(input.WalletSettings.ReturnWindowDays)
                    .AddDays(input.WalletSettings.ReleaseDelayDays);
                break;

            default:
                return CommissionAvailabilityDecision.Ineligible(
                    CommissionAvailabilityDecision.ReasonCodes.UnsupportedReleaseTrigger,
                    "The configured commission release trigger is not supported."
                );
        }

        if (input.IsOrderBacked && input.OrderPaymentStatus != PaymentStatus.Paid)
            return CommissionAvailabilityDecision.Pending(
                CommissionAvailabilityDecision.ReasonCodes.PaymentNotConfirmed,
                "Payment has not been confirmed."
            );

        if (input.IsOrderBacked && input.OrderPaidAt == null)
            return CommissionAvailabilityDecision.Pending(
                CommissionAvailabilityDecision.ReasonCodes.PaymentNotConfirmed,
                "The confirmed payment timestamp is missing."
            );

        if (input.CurrentTime < eligibleAt)
            return CommissionAvailabilityDecision.Pending(
                CommissionAvailabilityDecision.ReasonCodes.ReleaseDelayNotElapsed,
                "The commission release delay has not elapsed.",
                eligibleAt
            );

        return CommissionAvailabilityDecision.Releasable(eligibleAt);
    }
}
