using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Identity.Commands.RevokeAdministratorRole;

public sealed class RevokeAdministratorRoleCommandHandler(
    IApplicationDbContext db,
    IAdministratorAccountService administratorAccounts,
    IUser currentUser,
    IAuditWriter auditWriter
) : IRequestHandler<RevokeAdministratorRoleCommand>
{
    public async Task Handle(
        RevokeAdministratorRoleCommand request,
        CancellationToken cancellationToken
    )
    {
        if (currentUser.UserId == request.AdministratorUserId)
            throw new InvalidOperationException(
                "Administrators cannot revoke their own administrator role."
            );

        await administratorAccounts.StageAdministratorRoleRevocationAsync(
            request.OrganizationId,
            request.AdministratorUserId,
            cancellationToken
        );
        var audit = AuditCoverageMap.AdministratorRoleRevoked;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            request.AdministratorUserId,
            AuditJson.Serialize(new { IsAdministrator = true }),
            AuditJson.Serialize(new { IsAdministrator = false }),
            request.Reason.Trim()
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
