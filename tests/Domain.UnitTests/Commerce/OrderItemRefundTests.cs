using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Commerce;

public sealed class OrderItemRefundTests
{
    [Test]
    public void CreateSnapshotsTheServerCalculatedRefundAllocation()
    {
        var refund = CreateRefund(
            quantity: 1m,
            refundAmount: 250m,
            commissionableAmount: 200m,
            businessVolume: 50m,
            remainingQuantity: 4m
        );

        refund.Quantity.ShouldBe(1m);
        refund.RefundAmount.ShouldBe(250m);
        refund.CommissionableAmountToReverse.ShouldBe(200m);
        refund.BusinessVolumeToReverse.ShouldBe(50m);
        refund.Status.ShouldBe(OrderItemRefundStatus.Pending);
        refund.CompletedAt.ShouldBeNull();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateRejectsNonPositiveQuantity(decimal quantity)
    {
        Should.Throw<DomainInvariantException>(() => CreateRefund(quantity, 250m, 200m, 50m, 4m));
    }

    [Test]
    public void CreateRejectsQuantityAboveTheRemainingRefundableQuantity()
    {
        Should.Throw<DomainInvariantException>(() => CreateRefund(5m, 1_250m, 1_000m, 250m, 4m));
    }

    [Test]
    public void MarkReversedIsIdempotentAndRaisesOneDomainEvent()
    {
        var refund = CreateRefund(1m, 250m, 200m, 50m, 4m);
        var completedAt = DateTimeOffset.UtcNow;

        refund.MarkReversed(completedAt);
        refund.MarkReversed(completedAt.AddMinutes(1));

        refund.Status.ShouldBe(OrderItemRefundStatus.Reversed);
        refund.CompletedAt.ShouldBe(completedAt);
        refund.ReversalFailure.ShouldBeNull();
        refund.DomainEvents.Count.ShouldBe(1);
        refund.DomainEvents.Single().ShouldBeOfType<OrderItemRefundedEvent>();
    }

    [Test]
    public void CompletedRefundCannotBeMarkedAsFailed()
    {
        var refund = CreateRefund(1m, 250m, 200m, 50m, 4m);
        refund.MarkReversed(DateTimeOffset.UtcNow);

        Should.Throw<DomainInvariantException>(() =>
            refund.MarkReversalFailed("ledger failure", DateTimeOffset.UtcNow)
        );
    }

    private static OrderItemRefund CreateRefund(
        decimal quantity,
        decimal refundAmount,
        decimal commissionableAmount,
        decimal businessVolume,
        decimal remainingQuantity
    ) =>
        OrderItemRefund.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantity,
            refundAmount,
            commissionableAmount,
            businessVolume,
            remainingQuantity
        );
}
