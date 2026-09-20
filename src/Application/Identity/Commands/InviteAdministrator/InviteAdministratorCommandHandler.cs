using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Identity.Commands.InviteAdministrator;

public sealed class InviteAdministratorCommandHandler(
    IUser currentUser,
    IApplicationDbContext context,
    IAdministratorAccountService administratorAccountService,
    IAdministratorInvitationTokenService tokenService,
    IInvitationDeliveryOutbox deliveryOutbox,
    IAdministratorInvitationPolicy invitationPolicy,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<InviteAdministratorCommand, Guid>
{
    public async Task<Guid> Handle(
        InviteAdministratorCommand request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var mayManageOrganization = await administratorAccountService.CanManageOrganizationAsync(
            currentUserId,
            request.OrganizationId,
            cancellationToken
        );

        if (!mayManageOrganization)
            throw new ForbiddenAccessException();

        if (!Guid.TryParse(currentUserId, out var invitingAdministratorId))
            throw new UnauthorizedAccessException();

        var organizationExists = await context.Organizations.AnyAsync(
            organization => organization.Id == request.OrganizationId,
            cancellationToken
        );

        if (!organizationExists)
            throw new KeyNotFoundException("Organization was not found.");

        var normalizedEmail = administratorAccountService.NormalizeEmail(request.Email);
        var administratorExists = await administratorAccountService.AdministratorExistsAsync(
            request.OrganizationId,
            normalizedEmail,
            cancellationToken
        );

        if (administratorExists)
            throw new InvalidOperationException("Administrator already exists.");

        var existingInvitation = await context.AdministratorInvitations.SingleOrDefaultAsync(
            invitation =>
                invitation.OrganizationId == request.OrganizationId
                && invitation.NormalizedEmail == normalizedEmail
                && invitation.Status == AdministratorInvitationStatus.Pending,
            cancellationToken
        );

        var tokenPair = tokenService.GenerateToken();
        var now = clock.GetUtcNow();
        var invitation = existingInvitation;
        string? beforeJson = null;
        if (invitation is null)
        {
            invitation = AdministratorInvitation.Create(
                request.OrganizationId,
                request.Email,
                normalizedEmail,
                tokenPair.TokenHash,
                invitingAdministratorId,
                now,
                now.Add(invitationPolicy.Lifetime)
            );
            context.AdministratorInvitations.Add(invitation);
        }
        else
        {
            beforeJson = Serialize(invitation);
            invitation.Renew(tokenPair.TokenHash, now, now.Add(invitationPolicy.Lifetime));
        }
        var audit = AuditCoverageMap.AdministratorInvitationCreated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            invitation.Id,
            beforeJson,
            Serialize(invitation),
            null
        );
        await deliveryOutbox.StageAsync(
            invitation.Id,
            invitation.OrganizationId,
            invitation.Email,
            tokenPair.RawToken,
            "administrator-invitation",
            invitation.ExpiresAt,
            cancellationToken
        );
        await context.SaveChangesAsync(cancellationToken);

        return invitation.Id;
    }

    private static string Serialize(AdministratorInvitation invitation) =>
        AuditJson.Serialize(
            new
            {
                invitation.Status,
                invitation.InvitedByUserId,
                invitation.InvitedAt,
                invitation.ExpiresAt,
            }
        );
}
