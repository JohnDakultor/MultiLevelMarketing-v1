using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Identity.Commands.ExpireAdministratorInvitations.Models;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Identity.Commands.ExpireAdministratorInvitations;

public sealed class ExpireAdministratorInvitationsCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<ExpireAdministratorInvitationsCommand, ExpireAdministratorInvitationsResult>
{
    public async Task<ExpireAdministratorInvitationsResult> Handle(
        ExpireAdministratorInvitationsCommand request,
        CancellationToken cancellationToken
    )
    {
        var invitations = await db
            .AdministratorInvitations.Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.Status == AdministratorInvitationStatus.Pending
                && x.ExpiresAt <= request.AsOf
            )
            .OrderBy(x => x.ExpiresAt)
            .ThenBy(x => x.Id)
            .Take(request.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var invitation in invitations)
        {
            var beforeJson = Serialize(invitation);
            invitation.MarkExpired(request.AsOf);
            var audit = AuditCoverageMap.AdministratorInvitationExpired;
            auditWriter.Write(
                request.OrganizationId,
                audit.Action,
                audit.EntityType,
                invitation.Id,
                beforeJson,
                Serialize(invitation),
                null
            );
        }

        await db.SaveChangesAsync(cancellationToken);
        var remaining = await db.AdministratorInvitations.CountAsync(
            x =>
                x.OrganizationId == request.OrganizationId
                && x.Status == AdministratorInvitationStatus.Pending
                && x.ExpiresAt <= request.AsOf,
            cancellationToken
        );
        return new(invitations.Count, remaining, request.AsOf);
    }

    private static string Serialize(AdministratorInvitation invitation) =>
        AuditJson.Serialize(
            new
            {
                invitation.Status,
                invitation.InvitedAt,
                invitation.ExpiresAt,
            }
        );
}
