using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Identity.Queries.GetCurrentUser.Models;

namespace modular_mlm.Application.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    IApplicationDbContext db,
    IUser currentUser,
    ICurrentIdentityReader identityReader
) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException();

        var identity = await identityReader.GetByUserIdAsync(userId, cancellationToken);
        if (identity is null)
            throw new UnauthorizedAccessException("The authenticated account no longer exists.");

        Guid? customerId = null;
        Guid? agentId = null;
        if (identity.OrganizationId is { } organizationId)
        {
            customerId = await db
                .CustomerProfiles.AsNoTracking()
                .Where(profile =>
                    profile.OrganizationId == organizationId && profile.UserId == userId
                )
                .Select(profile => (Guid?)profile.Id)
                .SingleOrDefaultAsync(cancellationToken);

            agentId = await db
                .Agents.AsNoTracking()
                .Where(agent => agent.OrganizationId == organizationId && agent.UserId == userId)
                .Select(agent => (Guid?)agent.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return new CurrentUserDto(
            identity.UserId,
            identity.Email,
            identity.DisplayName,
            identity.Roles.Distinct(StringComparer.Ordinal).Order().ToArray(),
            identity.OrganizationId,
            customerId,
            agentId,
            identity.EmailVerified,
            identity.MfaEnabled
        );
    }
}
