using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Payouts.Commands.VerifyPayoutAccount;

public sealed class VerifyPayoutAccountCommandHandler(
    IUser currentUser,
    IAdministratorAccountService administrators,
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<VerifyPayoutAccountCommand>
{
    public async Task Handle(
        VerifyPayoutAccountCommand request,
        CancellationToken cancellationToken
    )
    {
        await RequireAdministratorAsync(request.OrganizationId, cancellationToken);
        var account = await db.PayoutAccounts.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PayoutAccountId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (account is null)
            throw new KeyNotFoundException("Payout account was not found.");
        var beforeJson = AuditJson.Serialize(new { account.VerificationStatus });
        account.Verify();
        var audit = AuditCoverageMap.PayoutAccountVerified;
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

    private async Task RequireAdministratorAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (
            !await administrators.CanManageOrganizationAsync(
                userId,
                organizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();
    }
}
