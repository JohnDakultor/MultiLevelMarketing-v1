using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Identity.Queries.GetAdministratorInvitations.Models;

namespace modular_mlm.Application.Identity.Queries.GetAdministratorInvitations;

public sealed class GetAdministratorInvitationsQueryHandler(
    IApplicationDbContext context,
    IUser currentUser,
    IAdministratorAccountService administratorAccountService
) : IRequestHandler<GetAdministratorInvitationsQuery, List<AdministratorInvitationDto>>
{
    public async Task<List<AdministratorInvitationDto>> Handle(
        GetAdministratorInvitationsQuery request,
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

        return await context
            .AdministratorInvitations.AsNoTracking()
            .Where(invitation =>
                invitation.OrganizationId == request.OrganizationId
                && invitation.Status == request.Status
            )
            .OrderByDescending(invitation => invitation.InvitedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(invitation => new AdministratorInvitationDto(
                invitation.Id,
                invitation.OrganizationId,
                invitation.InvitedByUserId,
                invitation.Email,
                invitation.InvitedAt,
                invitation.ExpiresAt,
                invitation.Status,
                invitation.AcceptedByUserId,
                invitation.AcceptedAt,
                invitation.RevokedByUserId,
                invitation.RevokedAt,
                invitation.RevocationReason
            ))
            .ToListAsync(cancellationToken);
    }
}
