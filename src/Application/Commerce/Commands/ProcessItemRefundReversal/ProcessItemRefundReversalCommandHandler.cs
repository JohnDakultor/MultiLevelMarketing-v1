using System.Text.Json;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;

public sealed class ProcessItemRefundReversalCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<ProcessItemRefundReversalCommand, bool>
{
    public async Task<bool> Handle(
        ProcessItemRefundReversalCommand request,
        CancellationToken cancellationToken
    )
    {
        var itemRefund = await db.OrderItemRefunds.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.OrderItemRefundId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (itemRefund is null)
            throw new KeyNotFoundException("Order item refund was not found.");
        if (itemRefund.Status == OrderItemRefundStatus.Reversed)
            return false;

        var paymentRefund = await db.PaymentRefunds.SingleAsync(
            candidate => candidate.Id == itemRefund.PaymentRefundId,
            cancellationToken
        );
        if (paymentRefund.Status != PaymentRefundStatus.Succeeded)
            throw new InvalidOperationException("The provider refund has not succeeded.");

        var order = await db
            .Orders.Include(candidate => candidate.Items)
            .SingleAsync(candidate => candidate.Id == itemRefund.OrderId, cancellationToken);
        var orderItem = order.Items.Single(candidate => candidate.Id == itemRefund.OrderItemId);
        var payment = await db.Payments.SingleAsync(
            candidate => candidate.Id == paymentRefund.PaymentId,
            cancellationToken
        );

        await ReverseDirectCommissionsAsync(itemRefund, orderItem, cancellationToken);
        await ReverseBinaryVolumeAsync(itemRefund, orderItem, cancellationToken);

        payment.ReconcileRefundedAmount(payment.RefundedAmount + paymentRefund.Amount);
        order.Refund(partial: payment.RefundedAmount < payment.Amount);
        itemRefund.MarkReversed(clock.GetUtcNow());

        auditWriter.Write(
            request.OrganizationId,
            AuditCoverageMap.OrderItemRefundReversed.Action,
            AuditCoverageMap.OrderItemRefundReversed.EntityType,
            itemRefund.Id,
            JsonSerializer.Serialize(new { Status = OrderItemRefundStatus.Pending }),
            JsonSerializer.Serialize(
                new
                {
                    itemRefund.Status,
                    itemRefund.Quantity,
                    itemRefund.RefundAmount,
                    itemRefund.BusinessVolumeToReverse,
                }
            ),
            paymentRefund.Reason
        );

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ReverseDirectCommissionsAsync(
        OrderItemRefund itemRefund,
        OrderItem orderItem,
        CancellationToken cancellationToken
    )
    {
        if (orderItem.CommissionableAmount <= 0m)
            return;
        var ratio = itemRefund.CommissionableAmountToReverse / orderItem.CommissionableAmount;
        var sources = await db
            .CommissionTransactions.Where(transaction =>
                transaction.OrganizationId == itemRefund.OrganizationId
                && transaction.SourceOrderItemId == itemRefund.OrderItemId
                && transaction.Type != CommissionType.Reversal
            )
            .ToListAsync(cancellationToken);

        foreach (var source in sources)
        {
            var alreadyApplied = await db.CommissionTransactions.AnyAsync(
                reversal =>
                    reversal.ReversalOfCommissionId == source.Id
                    && reversal.SourceOrderItemRefundId == itemRefund.Id,
                cancellationToken
            );
            if (alreadyApplied)
                continue;

            var reversedAmount = await db
                .CommissionTransactions.Where(reversal =>
                    reversal.ReversalOfCommissionId == source.Id
                )
                .SumAsync(reversal => -reversal.Amount, cancellationToken);
            var amount = Math.Min(
                source.Amount - reversedAmount,
                decimal.Round(source.Amount * ratio, 2, MidpointRounding.AwayFromZero)
            );
            if (amount <= 0m)
                continue;
            var baseAmount = decimal.Round(
                source.BaseAmount * ratio,
                2,
                MidpointRounding.AwayFromZero
            );
            var reversal = source.ReverseForRefund(
                itemRefund.Id,
                baseAmount,
                amount,
                reversedAmount + amount >= source.Amount
            );
            db.CommissionTransactions.Add(reversal);

            var walletEntry = await db.WalletEntries.SingleOrDefaultAsync(
                entry => entry.SourceType == "Commission" && entry.SourceId == source.Id,
                cancellationToken
            );
            if (walletEntry is not null)
                db.WalletEntries.Add(walletEntry.ReverseForRefund(itemRefund.Id, amount));
        }
    }

    private async Task ReverseBinaryVolumeAsync(
        OrderItemRefund itemRefund,
        OrderItem orderItem,
        CancellationToken cancellationToken
    )
    {
        if (orderItem.BusinessVolume <= 0m)
            return;
        var ratio = itemRefund.BusinessVolumeToReverse / orderItem.BusinessVolume;
        var credits = await db
            .BinaryVolumeEntries.Where(entry =>
                entry.OrganizationId == itemRefund.OrganizationId
                && entry.SourceOrderItemId == itemRefund.OrderItemId
                && entry.EntryType == BinaryVolumeEntryType.Credit
            )
            .ToListAsync(cancellationToken);
        var balances = await db
            .BinaryVolumeBalances.Where(balance =>
                balance.OrganizationId == itemRefund.OrganizationId
                && credits.Select(credit => credit.OwnerAgentId).Contains(balance.AgentId)
            )
            .ToDictionaryAsync(balance => balance.AgentId, cancellationToken);

        foreach (var credit in credits)
        {
            var alreadyApplied = await db.BinaryVolumeEntries.AnyAsync(
                reversal =>
                    reversal.ReversalOfEntryId == credit.Id
                    && reversal.SourceOrderItemRefundId == itemRefund.Id,
                cancellationToken
            );
            if (alreadyApplied)
                continue;
            var volume = decimal.Round(credit.Volume * ratio, 4, MidpointRounding.AwayFromZero);
            if (volume <= 0m)
                continue;
            db.BinaryVolumeEntries.Add(
                credit.ReverseForRefund(itemRefund.Id, volume, clock.GetUtcNow())
            );
            if (balances.TryGetValue(credit.OwnerAgentId, out var balance))
                balance.ReverseCredit(credit.Side, volume);
        }
    }
}
