using modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Commands.RequestItemRefund;

public sealed class RequestItemRefundCommandHandler(
    IApplicationDbContext db,
    IPaymentGateway gateway,
    ISender sender,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<RequestItemRefundCommand, Guid>
{
    public async Task<Guid> Handle(
        RequestItemRefundCommand request,
        CancellationToken cancellationToken
    )
    {
        var order = await db
            .Orders.Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.OrderId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (order is null)
            throw new KeyNotFoundException("Order was not found.");
        if (order.PaymentStatus is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
            throw new InvalidOperationException("Only a paid order can be refunded.");

        var item = order.Items.SingleOrDefault(candidate => candidate.Id == request.OrderItemId);
        if (item is null)
            throw new KeyNotFoundException("Order item was not found.");

        var payment = await db.Payments.SingleOrDefaultAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.OrderId == request.OrderId,
            cancellationToken
        );
        if (payment?.ProviderPaymentId is null)
            throw new InvalidOperationException("The provider payment has not been confirmed.");

        var normalizedReason = request.Reason.Trim().ToLowerInvariant();
        var pendingRequest = await (
            from existingItemRefund in db.OrderItemRefunds.AsNoTracking()
            join existingPaymentRefund in db.PaymentRefunds.AsNoTracking()
                on existingItemRefund.PaymentRefundId equals existingPaymentRefund.Id
            where
                existingItemRefund.OrganizationId == request.OrganizationId
                && existingItemRefund.OrderItemId == request.OrderItemId
                && existingPaymentRefund.Status == PaymentRefundStatus.Pending
            select new
            {
                existingItemRefund.Id,
                existingItemRefund.Quantity,
                existingPaymentRefund.Reason,
            }
        ).SingleOrDefaultAsync(cancellationToken);
        if (pendingRequest is not null)
        {
            if (
                pendingRequest.Quantity == request.Quantity
                && string.Equals(
                    pendingRequest.Reason,
                    normalizedReason,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return pendingRequest.Id;

            throw new InvalidOperationException(
                "This item already has a refund awaiting provider confirmation."
            );
        }

        var allocatedQuantity = await (
            from existingItemRefund in db.OrderItemRefunds.AsNoTracking()
            join existingPaymentRefund in db.PaymentRefunds.AsNoTracking()
                on existingItemRefund.PaymentRefundId equals existingPaymentRefund.Id
            where
                existingItemRefund.OrganizationId == request.OrganizationId
                && existingItemRefund.OrderItemId == request.OrderItemId
                && existingPaymentRefund.Status != PaymentRefundStatus.Failed
            select existingItemRefund.Quantity
        ).SumAsync(cancellationToken);
        var remainingQuantity = item.Quantity - allocatedQuantity;
        if (request.Quantity > remainingQuantity)
            throw new InvalidOperationException("Refund quantity exceeds the remaining quantity.");

        var refundAmount = Prorate(item.LineTotal, item.Quantity, request.Quantity, 2);
        var commissionable = Prorate(item.CommissionableAmount, item.Quantity, request.Quantity, 2);
        var businessVolume = Prorate(item.BusinessVolume, item.Quantity, request.Quantity, 4);
        if (refundAmount > payment.Amount - payment.RefundedAmount)
            throw new InvalidOperationException("Refund amount exceeds the payment balance.");

        var paymentRefund = PaymentRefund.Request(
            request.OrganizationId,
            payment.Id,
            order.Id,
            refundAmount,
            payment.Currency,
            normalizedReason,
            clock.GetUtcNow()
        );
        var itemRefund = OrderItemRefund.Create(
            request.OrganizationId,
            order.Id,
            item.Id,
            paymentRefund.Id,
            request.Quantity,
            refundAmount,
            commissionable,
            businessVolume,
            remainingQuantity
        );
        db.PaymentRefunds.Add(paymentRefund);
        db.OrderItemRefunds.Add(itemRefund);
        var audit = AuditCoverageMap.OrderItemRefundRequested;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            itemRefund.Id,
            null,
            AuditJson.Serialize(
                new
                {
                    itemRefund.OrderId,
                    itemRefund.OrderItemId,
                    itemRefund.Quantity,
                    itemRefund.RefundAmount,
                    itemRefund.Status,
                    itemRefund.RequestedAt,
                }
            ),
            string.IsNullOrWhiteSpace(request.AuditReason)
                ? request.Reason.Trim()
                : request.AuditReason.Trim()
        );
        await db.SaveChangesAsync(cancellationToken);

        var providerResult = await gateway.RefundAsync(
            new CreatePaymentRefundRequest(
                payment.ProviderPaymentId,
                ToMinorUnits(refundAmount),
                paymentRefund.Reason,
                paymentRefund.IdempotencyKey
            ),
            cancellationToken
        );
        paymentRefund.AttachProviderResult(
            providerResult.ProviderRefundId,
            providerResult.Status,
            clock.GetUtcNow()
        );
        await db.SaveChangesAsync(cancellationToken);

        if (paymentRefund.Status == PaymentRefundStatus.Succeeded)
            await sender.Send(
                new ProcessItemRefundReversalCommand(request.OrganizationId, itemRefund.Id),
                cancellationToken
            );

        return itemRefund.Id;
    }

    private static decimal Prorate(
        decimal total,
        int quantity,
        decimal refundQuantity,
        int scale
    ) => decimal.Round(total / quantity * refundQuantity, scale, MidpointRounding.AwayFromZero);

    private static long ToMinorUnits(decimal amount) => decimal.ToInt64(amount * 100m);
}
