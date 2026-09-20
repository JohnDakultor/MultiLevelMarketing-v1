using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Identity.Commands.RevokeCurrentSession;

public sealed class RevokeCurrentSessionCommandHandler(
    IAuthenticationSessionService sessions,
    IUser currentUser,
    TimeProvider clock
) : IRequestHandler<RevokeCurrentSessionCommand>
{
    public async Task Handle(
        RevokeCurrentSessionCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException();

        await sessions.RevokeSessionAsync(
            userId,
            request.SessionId,
            clock.GetUtcNow(),
            "user_logout",
            cancellationToken
        );
    }
}
