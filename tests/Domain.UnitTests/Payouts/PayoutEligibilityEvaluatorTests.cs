using Domain.Enums;
using Domain.Services;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.Wallets;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Payouts;

public sealed class PayoutEligibilityEvaluatorTests
{
    private readonly PayoutEligibilityEvaluator _evaluator = new();

    [Test]
    public void ActiveAgentAtMinimumWithAvailableFundsIsEligible()
    {
        var decision = _evaluator.Evaluate(CreateInput(requestedAmount: 100m));

        decision.IsEligible.ShouldBeTrue();
        decision.ReasonCode.ShouldBe(PayoutReasonCodes.Eligible);
        decision.AvailableAmount.ShouldBe(500m);
        decision.MinimumPayoutAmount.ShouldBe(100m);
        decision.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [TestCase(AgentStatus.Applied)]
    [TestCase(AgentStatus.PendingApproval)]
    [TestCase(AgentStatus.Inactive)]
    [TestCase(AgentStatus.Suspended)]
    [TestCase(AgentStatus.Closed)]
    public void NonActiveAgentIsRejected(AgentStatus status)
    {
        AssertIneligible(CreateInput(agentStatus: status), PayoutReasonCodes.AgentNotActive);
    }

    [TestCase(WalletStatus.Held)]
    [TestCase(WalletStatus.Closed)]
    public void NonActiveWalletIsRejected(WalletStatus status)
    {
        AssertIneligible(CreateInput(walletStatus: status), PayoutReasonCodes.WalletNotActive);
    }

    [Test]
    public void UnverifiedPayoutAccountIsRejected()
    {
        AssertIneligible(
            CreateInput(hasVerifiedAccount: false),
            PayoutReasonCodes.PayoutAccountNotVerified
        );
    }

    [Test]
    public void ComplianceOrAdministratorHoldIsRejected()
    {
        AssertIneligible(CreateInput(hasComplianceHold: true), PayoutReasonCodes.ComplianceHold);
    }

    [Test]
    public void CurrencyComparisonIsCaseInsensitive()
    {
        _evaluator
            .Evaluate(CreateInput(requestedCurrency: "php", walletCurrency: "PHP"))
            .IsEligible.ShouldBeTrue();

        AssertIneligible(
            CreateInput(requestedCurrency: "USD", walletCurrency: "PHP"),
            PayoutReasonCodes.CurrencyMismatch
        );
    }

    [Test]
    public void AmountBelowMinimumIsRejectedButExactMinimumIsEligible()
    {
        AssertIneligible(
            CreateInput(requestedAmount: 99.99m),
            PayoutReasonCodes.BelowMinimumPayout
        );
        _evaluator.Evaluate(CreateInput(requestedAmount: 100m)).IsEligible.ShouldBeTrue();
    }

    [Test]
    public void RequestExceedingAvailableFundsIsRejectedEvenWhenNetIsPositive()
    {
        var balance = new WalletBalanceSnapshot(0m, 100m, 50m, 0m, 0m, 150m);

        AssertIneligible(
            CreateInput(requestedAmount: 125m, balance: balance),
            PayoutReasonCodes.InsufficientAvailableBalance
        );
    }

    [Test]
    public void RecoverableNegativeBalanceIsRejectedWhenRecoveryIsDisabled()
    {
        var balance = new WalletBalanceSnapshot(0m, 500m, 0m, 0m, 25m, 475m);

        AssertIneligible(
            CreateInput(balance: balance),
            PayoutReasonCodes.NegativeBalanceNotAllowed
        );
    }

    [Test]
    public void RecoverableNegativeBalanceBeyondConfiguredMaximumIsRejected()
    {
        var balance = new WalletBalanceSnapshot(0m, 500m, 0m, 0m, 101m, 399m);

        AssertIneligible(
            CreateInput(balance: balance, maximumNegativeBalance: 100m),
            PayoutReasonCodes.MaximumNegativeBalanceExceeded
        );
    }

    private void AssertIneligible(PayoutEligibilityInput input, string expectedReasonCode)
    {
        var decision = _evaluator.Evaluate(input);

        decision.IsEligible.ShouldBeFalse();
        decision.ReasonCode.ShouldBe(expectedReasonCode);
        decision.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    private static PayoutEligibilityInput CreateInput(
        AgentStatus agentStatus = AgentStatus.Active,
        WalletStatus walletStatus = WalletStatus.Active,
        bool hasVerifiedAccount = true,
        decimal requestedAmount = 100m,
        string requestedCurrency = "PHP",
        string walletCurrency = "PHP",
        WalletBalanceSnapshot? balance = null,
        bool hasComplianceHold = false,
        decimal maximumNegativeBalance = 0m
    )
    {
        var allowsNegativeBalance = maximumNegativeBalance > 0m;
        var settings = WalletSettings.Create(
            CommissionReleaseTrigger.PaymentConfirmed,
            0,
            0,
            100m,
            allowsNegativeBalance,
            maximumNegativeBalance
        );

        return new PayoutEligibilityInput(
            agentStatus,
            walletStatus,
            hasVerifiedAccount,
            requestedAmount,
            requestedCurrency,
            walletCurrency,
            balance ?? new WalletBalanceSnapshot(0m, 500m, 0m, 0m, 0m, 500m),
            settings,
            hasComplianceHold
        );
    }
}
