using Domain.Services;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Wallets;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Wallets;

public sealed class WalletBalanceCalculatorTests
{
    [Test]
    public void ReleaseCreatesAnAvailableCreditLinkedToThePendingCredit()
    {
        var availableAt = DateTimeOffset.UtcNow;
        var pendingCredit = CreatePendingCredit();

        var releasedCredit = pendingCredit.Release(availableAt);

        releasedCredit.WalletId.ShouldBe(pendingCredit.WalletId);
        releasedCredit.Type.ShouldBe(WalletEntryType.AvailableCredit);
        releasedCredit.Amount.ShouldBe(pendingCredit.Amount);
        releasedCredit.SourceType.ShouldBe(pendingCredit.SourceType);
        releasedCredit.SourceId.ShouldBe(pendingCredit.SourceId);
        releasedCredit.AvailableAt.ShouldBe(availableAt);
        releasedCredit.ReleasedFromEntryId.ShouldBe(pendingCredit.Id);
        releasedCredit.ReversalOfEntryId.ShouldBeNull();
    }

    [Test]
    public void ReleaseRejectsAnEntryThatIsNotAPendingCredit()
    {
        var availableCredit = WalletEntry.Create(
            Guid.NewGuid(),
            WalletEntryType.AvailableCredit,
            100m,
            "CommissionTransaction",
            Guid.NewGuid()
        );

        Should.Throw<DomainInvariantException>(() =>
            availableCredit.Release(DateTimeOffset.UtcNow)
        );
    }

    [Test]
    public void ReleasedPendingCreditMovesValueFromPendingToAvailableExactlyOnce()
    {
        var pendingCredit = CreatePendingCredit();
        var releasedCredit = pendingCredit.Release(DateTimeOffset.UtcNow);
        var calculator = new WalletBalanceCalculator();

        var balance = calculator.Calculate([pendingCredit, releasedCredit]);

        balance.Pending.ShouldBe(0m);
        balance.Available.ShouldBe(100m);
        balance.Net.ShouldBe(100m);
    }

    private static WalletEntry CreatePendingCredit() =>
        WalletEntry.Create(
            Guid.NewGuid(),
            WalletEntryType.PendingCredit,
            100m,
            "CommissionTransaction",
            Guid.NewGuid()
        );
}
