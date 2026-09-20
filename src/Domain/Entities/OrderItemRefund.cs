using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Commerce;

public sealed class OrderItemRefund : OrganizationEntity
{
    private OrderItemRefund() { }

    public Guid OrderId { get; private set; }

    public Guid OrderItemId { get; private set; }

    public Guid PaymentRefundId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal RefundAmount { get; private set; }

    public decimal CommissionableAmountToReverse { get; private set; }

    public decimal BusinessVolumeToReverse { get; private set; }

    public OrderItemRefundStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? ReversalFailure { get; private set; }

    public static OrderItemRefund Create(
        Guid organizationId,
        Guid orderId,
        Guid orderItemId,
        Guid paymentRefundId,
        decimal quantity,
        decimal refundAmount,
        decimal commissionableAmountToReverse,
        decimal businessVolumeToReverse,
        decimal remainingRefundableQuantity
    )
    {
        if (
            organizationId == Guid.Empty
            || orderId == Guid.Empty
            || orderItemId == Guid.Empty
            || paymentRefundId == Guid.Empty
        )
            throw new DomainInvariantException("Required identifiers cannot be empty.");

        if (
            quantity <= 0m
            || refundAmount <= 0m
            || commissionableAmountToReverse < 0m
            || businessVolumeToReverse < 0m
        )
            throw new DomainInvariantException(
                "Financial and quantity amounts must be positive or valid."
            );

        if (quantity > remainingRefundableQuantity)
            throw new DomainInvariantException(
                "Refund quantity exceeds the item's remaining refundable quantity."
            );

        var refund = new OrderItemRefund
        {
            OrganizationId = organizationId,
            OrderId = orderId,
            OrderItemId = orderItemId,
            PaymentRefundId = paymentRefundId,
            Quantity = quantity,
            RefundAmount = refundAmount,
            CommissionableAmountToReverse = commissionableAmountToReverse,
            BusinessVolumeToReverse = businessVolumeToReverse,
            Status = OrderItemRefundStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow,
        };
        return refund;
    }

    public void MarkReversed(DateTimeOffset completedAt)
    {
        if (Status == OrderItemRefundStatus.Reversed)
            return;

        Status = OrderItemRefundStatus.Reversed;
        CompletedAt = completedAt;
        ReversalFailure = null;

        AddDomainEvent(
            new OrderItemRefundedEvent(
                OrganizationId: OrganizationId,
                OrderId: OrderId,
                OrderItemId: OrderItemId,
                OrderItemRefundId: Id,
                PaymentRefundId: PaymentRefundId
            )
        );
    }

    public void MarkReversalFailed(string failureMessage, DateTimeOffset failedAt)
    {
        if (Status == OrderItemRefundStatus.Reversed)
            throw new DomainInvariantException("A completed reversal cannot be marked failed.");

        Status = OrderItemRefundStatus.ReversalFailed;
        CompletedAt = failedAt;
        ReversalFailure = string.IsNullOrWhiteSpace(failureMessage)
            ? "No failure reason provided."
            : failureMessage.Trim();
    }
}
