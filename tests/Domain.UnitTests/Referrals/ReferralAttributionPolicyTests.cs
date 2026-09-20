using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Referral;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Referrals;

public sealed class ReferralAttributionPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void ShouldReturnNullWhenNoAttributionExists()
    {
        var result = Resolve(ReferralSettings.Default(), null, null);

        result.ShouldBeNull();
    }

    [Test]
    public void ShouldRetainAnExistingAttributionInsideTheWindow()
    {
        var existing = Attribution(Guid.NewGuid(), "EXISTING", Now.AddDays(-10));

        var result = Resolve(ReferralSettings.Default(), null, existing);

        result.ShouldBeSameAs(existing);
    }

    [Test]
    public void ShouldDiscardAnExistingAttributionOutsideTheWindow()
    {
        var existing = Attribution(Guid.NewGuid(), "EXPIRED", Now.AddDays(-30));

        var result = Resolve(ReferralSettings.Default(), null, existing);

        result.ShouldBeNull();
    }

    [Test]
    public void ShouldRetainExistingAgentWhenOverrideIsDisabled()
    {
        var existing = Attribution(Guid.NewGuid(), "EXISTING", Now.AddDays(-10));
        var incoming = Attribution(Guid.NewGuid(), "INCOMING", Now.AddMinutes(-1));
        var settings = ReferralSettings.Create(30, false, true);

        var result = Resolve(settings, incoming, existing);

        result.ShouldBeSameAs(existing);
    }

    [Test]
    public void ShouldSelectIncomingAgentWhenOverrideIsEnabled()
    {
        var existing = Attribution(Guid.NewGuid(), "EXISTING", Now.AddDays(-10));
        var incoming = Attribution(Guid.NewGuid(), "INCOMING", Now.AddMinutes(-1));
        var settings = ReferralSettings.Create(30, true, true);

        var result = Resolve(settings, incoming, existing);

        result.ShouldBeSameAs(incoming);
    }

    [Test]
    public void ShouldRefreshCaptureForTheSameAgentWhenOverrideIsDisabled()
    {
        var agentId = Guid.NewGuid();
        var existing = Attribution(agentId, "SAME", Now.AddDays(-10));
        var incoming = Attribution(agentId, "SAME", Now.AddMinutes(-1));

        var result = Resolve(ReferralSettings.Default(), incoming, existing);

        result.ShouldBeSameAs(incoming);
    }

    [Test]
    public void ShouldLockExistingAgentAfterFirstPurchase()
    {
        var existing = Attribution(Guid.NewGuid(), "LOCKED", Now.AddDays(-31));
        var incoming = Attribution(Guid.NewGuid(), "INCOMING", Now.AddMinutes(-1));
        var settings = ReferralSettings.Create(30, true, true);

        var result = Resolve(settings, incoming, existing, true);

        result.ShouldBeSameAs(existing);
    }

    [Test]
    public void ShouldApplyNormalOverrideRulesWhenPurchaseLockIsDisabled()
    {
        var existing = Attribution(Guid.NewGuid(), "EXISTING", Now.AddDays(-10));
        var incoming = Attribution(Guid.NewGuid(), "INCOMING", Now.AddMinutes(-1));
        var settings = ReferralSettings.Create(30, true, false);

        var result = Resolve(settings, incoming, existing, true);

        result.ShouldBeSameAs(incoming);
    }

    private static ReferralAttributionContext? Resolve(
        ReferralSettings settings,
        ReferralAttributionContext? incoming,
        ReferralAttributionContext? existing,
        bool hasCompletedFirstPurchase = false
    ) =>
        ReferralAttributionPolicy.Resolve(
            settings,
            incoming,
            existing,
            hasCompletedFirstPurchase,
            Now
        );

    private static ReferralAttributionContext Attribution(
        Guid agentId,
        string referralCode,
        DateTimeOffset capturedAt
    ) =>
        ReferralAttributionContext.Capture(
            agentId,
            referralCode,
            capturedAt,
            AttributionSource.ReferralLink
        );
}
