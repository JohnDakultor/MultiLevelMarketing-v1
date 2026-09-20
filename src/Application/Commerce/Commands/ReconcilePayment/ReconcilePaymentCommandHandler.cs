using modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Inventory.Commands.FinalizeOrderInventory;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Commands.ReconcilePayment;

public sealed class ReconcilePaymentCommandHandler(
    IApplicationDbContext db,
    IPaymentGateway gateway,
    TimeProvider clock,
    ISender sender
) : IRequestHandler<ReconcilePaymentCommand, bool>
{
    public async Task<bool> Handle(
        ReconcilePaymentCommand request,
        CancellationToken cancellationToken
    )
    {
        var payment = await db.Payments.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PaymentId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (payment is null)
            throw new KeyNotFoundException("Payment was not found.");
        if (payment.ProviderPaymentId is null)
            throw new InvalidOperationException("Payment has no provider payment ID to reconcile.");

        var provider = await gateway.GetPaymentAsync(payment.ProviderPaymentId, cancellationToken);
        if (
            provider.AmountInMinorUnits != ToMinorUnits(payment.Amount)
            || !string.Equals(
                provider.Currency,
                payment.Currency,
                StringComparison.OrdinalIgnoreCase
            )
        )
            throw new InvalidOperationException(
                "Provider payment amount or currency does not match."
            );

        var order = await db
            .Orders.Include(candidate => candidate.Items)
            .SingleAsync(candidate => candidate.Id == payment.OrderId, cancellationToken);
        var changed = false;
        if (string.Equals(provider.Status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            changed =
                payment.Status != PaymentStatus.Paid
                || order.PaymentStatus == PaymentStatus.Pending;
            payment.MarkPaid(provider.ProviderPaymentId, provider.PaidAt ?? clock.GetUtcNow());
            if (order.PaymentStatus == PaymentStatus.Pending)
            {
                order.MarkPaid(provider.PaidAt ?? clock.GetUtcNow());
                await sender.Send(
                    new FinalizeOrderInventoryCommand(order.OrganizationId, order.Id),
                    cancellationToken
                );
            }
        }

        var providerRefunded = provider.RefundedAmountInMinorUnits / 100m;
        if (providerRefunded > payment.RefundedAmount)
        {
            var pendingRefunds = await db
                .PaymentRefunds.Where(refund =>
                    refund.PaymentId == payment.Id && refund.Status == PaymentRefundStatus.Pending
                )
                .OrderBy(refund => refund.RequestedAt)
                .ToListAsync(cancellationToken);
            foreach (var refund in pendingRefunds)
            {
                if (payment.RefundedAmount + refund.Amount > providerRefunded)
                    break;
                refund.MarkSucceeded(clock.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                var itemRefundId = await db
                    .OrderItemRefunds.Where(itemRefund => itemRefund.PaymentRefundId == refund.Id)
                    .Select(itemRefund => itemRefund.Id)
                    .SingleOrDefaultAsync(cancellationToken);
                if (itemRefundId != Guid.Empty)
                    await sender.Send(
                        new ProcessItemRefundReversalCommand(payment.OrganizationId, itemRefundId),
                        cancellationToken
                    );
                else
                {
                    if (refund.Amount != payment.Amount - payment.RefundedAmount)
                        throw new InvalidOperationException(
                            "A partial provider refund has no item-level allocation."
                        );
                    await ReverseCompensationAsync(payment, order, cancellationToken);
                    payment.ReconcileRefundedAmount(payment.RefundedAmount + refund.Amount);
                    order.Refund(partial: payment.RefundedAmount < payment.Amount);
                    await db.SaveChangesAsync(cancellationToken);
                }
                changed = true;
            }
            if (providerRefunded > payment.RefundedAmount)
                throw new InvalidOperationException(
                    "Provider refund total cannot be matched to pending refund allocations."
                );
        }

        return (await db.SaveChangesAsync(cancellationToken) > 0) || changed;
    }

    private async Task ReverseCompensationAsync(
        Domain.Payments.Payment payment,
        Order order,
        CancellationToken cancellationToken
    )
    {
        var commissions = await db
            .CommissionTransactions.Where(transaction =>
                transaction.OrganizationId == payment.OrganizationId
                && transaction.SourceOrderId == order.Id
                && transaction.Type != CommissionType.Reversal
                && transaction.Status != CommissionStatus.Reversed
            )
            .ToListAsync(cancellationToken);
        foreach (var commission in commissions)
        {
            var reversal = commission.Reverse();
            db.CommissionTransactions.Add(reversal);
            var walletEntry = await db.WalletEntries.SingleOrDefaultAsync(
                entry => entry.SourceType == "Commission" && entry.SourceId == commission.Id,
                cancellationToken
            );
            if (walletEntry is not null)
                db.WalletEntries.Add(walletEntry.Reverse());
        }

        var itemIds = order.Items.Select(item => item.Id).ToArray();
        var volumeCredits = await db
            .BinaryVolumeEntries.Where(entry =>
                entry.OrganizationId == payment.OrganizationId
                && entry.SourceOrderItemId.HasValue
                && itemIds.Contains(entry.SourceOrderItemId.Value)
                && entry.EntryType == BinaryVolumeEntryType.Credit
            )
            .ToListAsync(cancellationToken);
        var balances = await db
            .BinaryVolumeBalances.Where(balance =>
                balance.OrganizationId == payment.OrganizationId
                && volumeCredits.Select(entry => entry.OwnerAgentId).Contains(balance.AgentId)
            )
            .ToDictionaryAsync(balance => balance.AgentId, cancellationToken);
        foreach (var credit in volumeCredits)
        {
            db.BinaryVolumeEntries.Add(credit.Reverse(clock.GetUtcNow()));
            if (balances.TryGetValue(credit.OwnerAgentId, out var balance))
                balance.ReverseCredit(credit.Side, credit.Volume);
        }
    }

    private static long ToMinorUnits(decimal amount) => decimal.ToInt64(amount * 100m);
}
