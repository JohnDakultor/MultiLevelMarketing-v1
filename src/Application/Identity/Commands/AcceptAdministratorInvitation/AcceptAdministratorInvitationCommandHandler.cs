using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Identity.Commands.AcceptAdministratorInvitation;

public sealed class AcceptAdministratorInvitationCommandHandler(
    IApplicationDbContext context,
    IAdministratorInvitationTokenService administratorInvitationTokenService,
    IAdministratorAccountService administratorAccountService,
    IIdentityService identityService,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<AcceptAdministratorInvitationCommand, Guid>
{
    public async Task<Guid> Handle(
        AcceptAdministratorInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        var now = clock.GetUtcNow();
        var submittedTokenHash = administratorInvitationTokenService
            .HashToken(request.RawToken)
            .TokenHash;

        var invitation = await context.AdministratorInvitations.SingleOrDefaultAsync(
            candidate => candidate.TokenHash == submittedTokenHash,
            cancellationToken
        );

        if (invitation is null || invitation.Status != AdministratorInvitationStatus.Pending)
            throw InvalidInvitation();

        if (!invitation.CanBeAccepted(now))
        {
            var beforeJson = Serialize(invitation);
            invitation.MarkExpired(now);
            var expiredAudit = AuditCoverageMap.AdministratorInvitationExpired;
            auditWriter.Write(
                invitation.OrganizationId,
                expiredAudit.Action,
                expiredAudit.EntityType,
                invitation.Id,
                beforeJson,
                Serialize(invitation),
                null
            );
            await context.SaveChangesAsync(cancellationToken);
            throw InvalidInvitation();
        }

        var administratorUserId = await administratorAccountService.CreateAdministratorAsync(
            invitation.OrganizationId,
            invitation.Email,
            request.DisplayName,
            request.Password,
            cancellationToken
        );

        var invitationBefore = Serialize(invitation);
        invitation.Accept(administratorUserId, now);
        var acceptedAudit = AuditCoverageMap.AdministratorInvitationAccepted;
        auditWriter.WriteAsActor(
            invitation.OrganizationId,
            administratorUserId,
            acceptedAudit.Action,
            acceptedAudit.EntityType,
            invitation.Id,
            invitationBefore,
            Serialize(invitation),
            null
        );
        var roleAudit = AuditCoverageMap.AdministratorRoleGranted;
        auditWriter.WriteAsActor(
            invitation.OrganizationId,
            administratorUserId,
            roleAudit.Action,
            roleAudit.EntityType,
            administratorUserId,
            AuditJson.Serialize(new { IsAdministrator = false }),
            AuditJson.Serialize(new { IsAdministrator = true }),
            "Administrator invitation accepted."
        );

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await identityService.DeleteUserAsync(administratorUserId.ToString());
            throw;
        }

        return administratorUserId;
    }

    private static UnauthorizedAccessException InvalidInvitation() =>
        new("The administrator invitation is invalid or has expired.");

    private static string Serialize(AdministratorInvitation invitation) =>
        AuditJson.Serialize(
            new
            {
                invitation.Status,
                invitation.InvitedByUserId,
                invitation.InvitedAt,
                invitation.ExpiresAt,
                invitation.AcceptedByUserId,
                invitation.AcceptedAt,
            }
        );
}
