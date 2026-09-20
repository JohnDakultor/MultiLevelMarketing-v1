using modular_mlm.Application.Commerce.Commands.ReconcilePayment;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Payouts.Commands.ReconcilePayout;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payouts;

namespace modular_mlm.Application.Common.Commands.ReconcileProviderState;

public sealed class ReconcileProviderStateCommandHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<ReconcileProviderStateCommand, int>
{
    public async Task<int> Handle(
        ReconcileProviderStateCommand request,
        CancellationToken cancellationToken
    )
    {
        var paymentIds = await db
            .Payments.AsNoTracking()
            .Where(payment =>
                payment.ProviderPaymentId != null
                && (
                    payment.Status == PaymentStatus.Paid
                    || payment.Status == PaymentStatus.PartiallyRefunded
                )
            )
            .Select(payment => new { payment.Id, payment.OrganizationId })
            .ToListAsync(cancellationToken);
        var payoutIds = await db
            .PayoutRequests.AsNoTracking()
            .Where(payout =>
                payout.Status == PayoutStatus.Processing && payout.ProviderTransferId != null
            )
            .Select(payout => payout.Id)
            .ToListAsync(cancellationToken);

        var changes = 0;
        foreach (var payment in paymentIds)
            if (
                await sender.Send(
                    new ReconcilePaymentCommand(payment.OrganizationId, payment.Id),
                    cancellationToken
                )
            )
                changes++;
        foreach (var payoutId in payoutIds)
            if (await sender.Send(new ReconcilePayoutCommand(payoutId), cancellationToken))
                changes++;
        return changes;
    }
}
