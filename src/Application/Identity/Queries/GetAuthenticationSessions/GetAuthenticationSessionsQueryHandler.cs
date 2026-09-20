using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Identity.Queries.GetAuthenticationSessions;

public sealed class GetAuthenticationSessionsQueryHandler(
    IUser currentUser,
    ICurrentAuthenticationSession currentSession,
    IAuthenticationSessionService sessions
) : IRequestHandler<GetAuthenticationSessionsQuery, IReadOnlyList<AuthenticationSessionInfo>>
{
    public Task<IReadOnlyList<AuthenticationSessionInfo>> Handle(
        GetAuthenticationSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException();
        return sessions.GetSessionsAsync(userId, currentSession.SessionId, cancellationToken);
    }
}
