using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Identity.Commands.RevokeAdministratorInvitation;

public sealed class RevokeAdministratorInvitationCommandHandler(
    IApplicationDbContext db,
    IUser currentUser,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<RevokeAdministratorInvitationCommand>
{
    public async Task Handle(
        RevokeAdministratorInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        if (!Guid.TryParse(currentUser.Id, out var actorId))
            throw new UnauthorizedAccessException();
        var invitation = await db.AdministratorInvitations.SingleOrDefaultAsync(
            x => x.Id == request.InvitationId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (invitation is null)
            throw new KeyNotFoundException("Administrator invitation was not found.");

        var beforeJson = Serialize(invitation);
        invitation.Revoke(actorId, clock.GetUtcNow(), request.Reason);
        var audit = AuditCoverageMap.AdministratorInvitationRevoked;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            invitation.Id,
            beforeJson,
            Serialize(invitation),
            request.Reason.Trim()
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Serialize(AdministratorInvitation invitation) =>
        AuditJson.Serialize(
            new
            {
                invitation.Status,
                invitation.InvitedByUserId,
                invitation.InvitedAt,
                invitation.ExpiresAt,
                invitation.RevokedByUserId,
                invitation.RevokedAt,
            }
        );
}
