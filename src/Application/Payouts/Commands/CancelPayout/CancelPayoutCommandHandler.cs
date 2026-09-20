using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Payouts.Commands.CancelPayout;

public sealed class CancelPayoutCommandHandler(IUser currentUser, IApplicationDbContext db)
    : IRequestHandler<CancelPayoutCommand>
{
    public async Task Handle(CancelPayoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (
            !await db.Agents.AnyAsync(
                agent =>
                    agent.Id == request.AgentId
                    && agent.OrganizationId == request.OrganizationId
                    && agent.UserId == userId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();
        var payout = await db.PayoutRequests.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PayoutRequestId
                && candidate.OrganizationId == request.OrganizationId
                && candidate.AgentId == request.AgentId,
            cancellationToken
        );
        if (payout is null)
            throw new KeyNotFoundException("Payout request was not found.");
        payout.Cancel();
        var hold = await db.WalletEntries.SingleOrDefaultAsync(
            entry =>
                entry.SourceType == "PayoutRequest"
                && entry.SourceId == payout.Id
                && entry.Type == WalletEntryType.Hold,
            cancellationToken
        );
        if (hold is not null)
            db.WalletEntries.Add(hold.Reverse());
        await db.SaveChangesAsync(cancellationToken);
    }
}
