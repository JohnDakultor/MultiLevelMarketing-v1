using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Wallets;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Compensation;

public sealed class RefundReversalTests
{
    [Test]
    public void PartialCommissionReversalCreatesLinkedNegativeEntryWithoutMutatingOriginalAmount()
    {
        var refundId = Guid.NewGuid();
        var original = CommissionTransaction.CreateDirectSale(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "direct-sale",
            1_000m,
            0.10m,
            100m
        );

        var reversal = original.ReverseForRefund(refundId, 250m, 25m, fullyReversed: false);

        original.Amount.ShouldBe(100m);
        original.Status.ShouldBe(CommissionStatus.Pending);
        reversal.Amount.ShouldBe(-25m);
        reversal.BaseAmount.ShouldBe(250m);
        reversal.Type.ShouldBe(CommissionType.Reversal);
        reversal.ReversalOfCommissionId.ShouldBe(original.Id);
        reversal.SourceOrderItemRefundId.ShouldBe(refundId);
    }

    [Test]
    public void FullCommissionReversalFinalizesTheOriginalCommission()
    {
        var original = CommissionTransaction.CreateDirectSale(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "direct-sale",
            1_000m,
            0.10m,
            100m
        );

        original.ReverseForRefund(Guid.NewGuid(), 1_000m, 100m, fullyReversed: true);

        original.Status.ShouldBe(CommissionStatus.Reversed);
    }

    [Test]
    public void VolumeReversalCreatesASeparateLinkedNegativeEntry()
    {
        var refundId = Guid.NewGuid();
        var original = BinaryVolumeEntry.Credit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlacementSide.Left,
            80m,
            DateTimeOffset.UtcNow
        );

        var reversal = original.ReverseForRefund(refundId, 20m, DateTimeOffset.UtcNow);

        original.Volume.ShouldBe(80m);
        reversal.Volume.ShouldBe(-20m);
        reversal.EntryType.ShouldBe(BinaryVolumeEntryType.Reversal);
        reversal.ReversalOfEntryId.ShouldBe(original.Id);
        reversal.SourceOrderItemRefundId.ShouldBe(refundId);
    }

    [Test]
    public void WalletReversalCreatesARecoverableNegativeEntryLinkedToTheRefund()
    {
        var refundId = Guid.NewGuid();
        var original = WalletEntry.Create(
            Guid.NewGuid(),
            WalletEntryType.AvailableCredit,
            100m,
            "CommissionTransaction",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow
        );

        var reversal = original.ReverseForRefund(refundId, 25m);

        original.Amount.ShouldBe(100m);
        reversal.Amount.ShouldBe(-25m);
        reversal.Type.ShouldBe(WalletEntryType.Reversal);
        reversal.SourceType.ShouldBe("OrderItemRefund");
        reversal.SourceId.ShouldBe(refundId);
        reversal.ReversalOfEntryId.ShouldBe(original.Id);
    }

    [Test]
    public void ReversalCannotExceedItsOriginalLedgerEntry()
    {
        var original = WalletEntry.Create(
            Guid.NewGuid(),
            WalletEntryType.AvailableCredit,
            100m,
            "CommissionTransaction",
            Guid.NewGuid()
        );

        Should.Throw<DomainInvariantException>(() =>
            original.ReverseForRefund(Guid.NewGuid(), 100.01m)
        );
    }
}
