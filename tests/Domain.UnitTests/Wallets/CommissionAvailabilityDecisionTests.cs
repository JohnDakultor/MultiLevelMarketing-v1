using modular_mlm.Domain.Services;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Wallets;

public sealed class CommissionAvailabilityDecisionTests
{
    [Test]
    public void ReleasableDecisionContainsTheEligibilityTimestampAndStableReasonCode()
    {
        var eligibleAt = DateTimeOffset.UtcNow;

        var decision = CommissionAvailabilityDecision.Releasable(eligibleAt);

        decision.IsReleasable.ShouldBeTrue();
        decision.EligibleAt.ShouldBe(eligibleAt);
        decision.ReasonCode.ShouldBe(CommissionAvailabilityDecision.ReasonCodes.Releasable);
        decision.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void PendingDecisionPreservesItsExplanationAndOptionalEligibilityTimestamp()
    {
        var eligibleAt = DateTimeOffset.UtcNow.AddDays(1);

        var decision = CommissionAvailabilityDecision.Pending(
            CommissionAvailabilityDecision.ReasonCodes.ReleaseDelayNotElapsed,
            "The commission release delay has not elapsed.",
            eligibleAt
        );

        decision.IsReleasable.ShouldBeFalse();
        decision.EligibleAt.ShouldBe(eligibleAt);
        decision.ReasonCode.ShouldBe(
            CommissionAvailabilityDecision.ReasonCodes.ReleaseDelayNotElapsed
        );
    }

    [Test]
    public void IneligibleDecisionNeverProvidesAnEligibilityTimestamp()
    {
        var decision = CommissionAvailabilityDecision.Ineligible(
            CommissionAvailabilityDecision.ReasonCodes.SourceReversed,
            "The source transaction was reversed or fully refunded."
        );

        decision.IsReleasable.ShouldBeFalse();
        decision.EligibleAt.ShouldBeNull();
        decision.ReasonCode.ShouldBe(CommissionAvailabilityDecision.ReasonCodes.SourceReversed);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void DecisionRejectsAnEmptyReasonCode(string? reasonCode)
    {
        Should.Throw<ArgumentException>(() =>
            CommissionAvailabilityDecision.Pending(reasonCode!, "A reason")
        );
    }
}
