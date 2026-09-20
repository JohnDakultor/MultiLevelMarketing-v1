using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Payouts.Commands.RejectPayoutAccount;

public sealed class RejectPayoutAccountCommandHandler(
    IUser currentUser,
    IAdministratorAccountService administrators,
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<RejectPayoutAccountCommand>
{
    public async Task Handle(
        RejectPayoutAccountCommand request,
        CancellationToken cancellationToken
    )
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
        var account = await db.PayoutAccounts.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PayoutAccountId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (account is null)
            throw new KeyNotFoundException("Payout account was not found.");
        var beforeJson = AuditJson.Serialize(new { account.VerificationStatus });
        account.Reject();
        var audit = AuditCoverageMap.PayoutAccountRejected;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            account.Id,
            beforeJson,
            AuditJson.Serialize(new { account.VerificationStatus }),
            null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
