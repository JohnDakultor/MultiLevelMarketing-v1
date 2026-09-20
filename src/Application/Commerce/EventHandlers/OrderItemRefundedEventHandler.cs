using modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.EventHandlers;

public sealed class OrderItemRefundedEventHandler(IApplicationDbContext db, ISender sender)
    : INotificationHandler<OrderItemRefundedEvent>
{
    public async Task Handle(
        OrderItemRefundedEvent notification,
        CancellationToken cancellationToken
    )
    {
        var refund = await (
            from itemRefund in db.OrderItemRefunds.AsNoTracking()
            join paymentRefund in db.PaymentRefunds.AsNoTracking()
                on itemRefund.PaymentRefundId equals paymentRefund.Id
            join order in db.Orders.AsNoTracking() on itemRefund.OrderId equals order.Id
            join orderItem in db.OrderItems.AsNoTracking()
                on itemRefund.OrderItemId equals orderItem.Id
            where
                itemRefund.Id == notification.OrderItemRefundId
                && itemRefund.OrganizationId == notification.OrganizationId
                && itemRefund.OrderId == notification.OrderId
                && itemRefund.OrderItemId == notification.OrderItemId
                && itemRefund.PaymentRefundId == notification.PaymentRefundId
                && paymentRefund.OrganizationId == notification.OrganizationId
                && paymentRefund.OrderId == notification.OrderId
                && order.OrganizationId == notification.OrganizationId
                && orderItem.OrderId == notification.OrderId
            select new { itemRefund.Status, PaymentRefundStatus = paymentRefund.Status }
        ).SingleOrDefaultAsync(cancellationToken);

        if (refund is null)
            throw new InvalidOperationException(
                "The refund event does not match a complete Organization-owned refund chain."
            );
        if (refund.Status == OrderItemRefundStatus.Reversed)
            return;
        if (refund.PaymentRefundStatus != PaymentRefundStatus.Succeeded)
            throw new InvalidOperationException(
                "The provider refund must succeed before compensation is reversed."
            );

        await sender.Send(
            new ProcessItemRefundReversalCommand(
                notification.OrganizationId,
                notification.OrderItemRefundId
            ),
            cancellationToken
        );
    }
}
