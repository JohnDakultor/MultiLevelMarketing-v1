using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Payouts.Commands.RejectPayout;

public sealed class RejectPayoutCommandHandler(
    IUser currentUser,
    IAdministratorAccountService administrators,
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<RejectPayoutCommand>
{
    public async Task Handle(RejectPayoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (
            !await administrators.CanManageOrganizationAsync(
                userId,
                request.OrganizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();
        var payout = await db.PayoutRequests.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PayoutRequestId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (payout is null)
            throw new KeyNotFoundException("Payout request was not found.");
        var beforeJson = AuditJson.Serialize(new { payout.Status });
        payout.Reject();
        await ReleaseHoldAsync(payout.Id, cancellationToken);
        var audit = AuditCoverageMap.PayoutRejected;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            payout.Id,
            beforeJson,
            AuditJson.Serialize(new { payout.Status }),
            null
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ReleaseHoldAsync(Guid payoutId, CancellationToken token)
    {
        var hold = await db.WalletEntries.SingleOrDefaultAsync(
            entry =>
                entry.SourceType == "PayoutRequest"
                && entry.SourceId == payoutId
                && entry.Type == WalletEntryType.Hold,
            token
        );
        if (hold is not null)
            db.WalletEntries.Add(hold.Reverse());
    }
}
