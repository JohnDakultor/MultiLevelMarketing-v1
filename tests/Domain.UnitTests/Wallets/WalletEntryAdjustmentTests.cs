using Domain.Enums;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Wallets;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Wallets;

public sealed class WalletEntryAdjustmentTests
{
    [TestCase(WalletAdjustmentDirection.Credit, 125.50, 125.50)]
    [TestCase(WalletAdjustmentDirection.Debit, 125.50, -125.50)]
    public void CreateAdjustmentSignsAmountFromDirection(
        WalletAdjustmentDirection direction,
        decimal amount,
        decimal expectedAmount
    )
    {
        var walletId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var entry = WalletEntry.CreateAdjustment(
            walletId,
            direction,
            amount,
            " adjustment-1 ",
            createdAt
        );

        entry.WalletId.ShouldBe(walletId);
        entry.Type.ShouldBe(WalletEntryType.Adjustment);
        entry.Amount.ShouldBe(expectedAmount);
        entry.SourceType.ShouldBe("AdministratorAdjustment");
        entry.SourceId.ShouldBe(entry.Id);
        entry.IdempotencyKey.ShouldBe("adjustment-1");
        entry.AvailableAt.ShouldBe(createdAt);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateAdjustmentRejectsNonPositiveAmount(decimal amount)
    {
        Should.Throw<DomainInvariantException>(() =>
            WalletEntry.CreateAdjustment(
                Guid.NewGuid(),
                WalletAdjustmentDirection.Credit,
                amount,
                "adjustment-1",
                DateTimeOffset.UtcNow
            )
        );
    }

    [Test]
    public void GenericFactoryRejectsAdjustmentEntries()
    {
        Should.Throw<DomainInvariantException>(() =>
            WalletEntry.Create(
                Guid.NewGuid(),
                WalletEntryType.Adjustment,
                10m,
                "Manual",
                Guid.NewGuid()
            )
        );
    }
}
