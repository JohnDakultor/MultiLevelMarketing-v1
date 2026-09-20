using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Payouts.Commands.ReconcilePayout;

namespace modular_mlm.Application.Payouts.Commands.ReconcilePayoutByTransfer;

public sealed class ReconcilePayoutByTransferCommandHandler(
    IApplicationDbContext db,
    ISender sender
) : IRequestHandler<ReconcilePayoutByTransferCommand, bool>
{
    public async Task<bool> Handle(
        ReconcilePayoutByTransferCommand request,
        CancellationToken cancellationToken
    )
    {
        var payoutId = await db
            .PayoutRequests.AsNoTracking()
            .Where(payout => payout.ProviderTransferId == request.ProviderTransferId)
            .Select(payout => payout.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (payoutId == Guid.Empty)
            throw new KeyNotFoundException("PayMongo transfer is unknown.");
        return await sender.Send(new ReconcilePayoutCommand(payoutId), cancellationToken);
    }
}
