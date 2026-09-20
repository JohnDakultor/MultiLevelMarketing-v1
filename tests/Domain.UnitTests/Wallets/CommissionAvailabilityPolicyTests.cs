using Domain.Enums;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Services;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Wallets;

public sealed class CommissionAvailabilityPolicyTests
{
    private readonly CommissionAvailabilityPolicy _policy = new();

    [Test]
    public void PaidCommissionBeforeDelayRemainsPendingWithEligibilityTime()
    {
        var paidAt = DateTimeOffset.UtcNow;

        var decision = _policy.Evaluate(
            CreateInput(
                WalletSettings.Create(
                    CommissionReleaseTrigger.PaymentConfirmed,
                    2,
                    0,
                    10m,
                    false,
                    0m
                ),
                paidAt.AddDays(1),
                paidAt
            )
        );

        decision.IsReleasable.ShouldBeFalse();
        decision.EligibleAt.ShouldBe(paidAt.AddDays(2));
        decision.ReasonCode.ShouldBe(
            CommissionAvailabilityDecision.ReasonCodes.ReleaseDelayNotElapsed
        );
    }

    [Test]
    public void ExactEligibilityBoundaryIsReleasable()
    {
        var paidAt = DateTimeOffset.UtcNow;
        var settings = WalletSettings.Create(
            CommissionReleaseTrigger.PaymentConfirmed,
            2,
            0,
            10m,
            false,
            0m
        );

        var decision = _policy.Evaluate(CreateInput(settings, paidAt.AddDays(2), paidAt));

        decision.IsReleasable.ShouldBeTrue();
        decision.EligibleAt.ShouldBe(paidAt.AddDays(2));
    }

    [Test]
    public void ReturnWindowTriggerIncludesWindowAndReleaseDelay()
    {
        var deliveredAt = DateTimeOffset.UtcNow;
        var settings = WalletSettings.Create(
            CommissionReleaseTrigger.ReturnWindowElapsed,
            2,
            7,
            10m,
            false,
            0m
        );
        var input = CreateInput(settings, deliveredAt.AddDays(8), deliveredAt.AddDays(-2)) with
        {
            OrderStatus = OrderStatus.Delivered,
            OrderDeliveredAt = deliveredAt,
        };

        var decision = _policy.Evaluate(input);

        decision.IsReleasable.ShouldBeFalse();
        decision.EligibleAt.ShouldBe(deliveredAt.AddDays(9));
    }

    [Test]
    public void RefundedSourceIsIneligible()
    {
        var input = CreateInput(WalletSettings.Default(), DateTimeOffset.UtcNow) with
        {
            IsReversedOrRefunded = true,
        };

        var decision = _policy.Evaluate(input);

        decision.IsReleasable.ShouldBeFalse();
        decision.ReasonCode.ShouldBe(CommissionAvailabilityDecision.ReasonCodes.SourceReversed);
        decision.EligibleAt.ShouldBeNull();
    }

    private static CommissionAvailabilityPolicy.CommissionAvailabilityInput CreateInput(
        WalletSettings settings,
        DateTimeOffset currentTime,
        DateTimeOffset? paidAt = null
    ) =>
        new()
        {
            CommissionStatus = CommissionStatus.Pending,
            CommissionCreatedAt = currentTime.AddDays(-10),
            IsOrderBacked = true,
            OrderPaymentStatus = PaymentStatus.Paid,
            OrderPaidAt = paidAt ?? currentTime.AddDays(-1),
            OrderStatus = OrderStatus.Paid,
            WalletSettings = settings,
            CurrentTime = currentTime,
        };
}
