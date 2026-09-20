namespace modular_mlm.Application.Common.Interfaces;

using modular_mlm.Application.Common.Models;

public interface IAuthenticationSessionService
{
    Task<Guid> CreateOrRotateSessionAsync(
        string userId,
        Guid? currentSessionId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    );

    Task<bool> IsSessionActiveAsync(
        string userId,
        Guid sessionId,
        DateTimeOffset currentTime,
        CancellationToken cancellationToken
    );

    Task<bool> RevokeSessionAsync(
        string userId,
        Guid sessionId,
        DateTimeOffset revokedAt,
        string reason,
        CancellationToken cancellationToken
    );

    Task<int> RevokeAllSessionsAsync(
        string userId,
        DateTimeOffset revokedAt,
        string reason,
        Guid? exceptSessionId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<AuthenticationSessionInfo>> GetSessionsAsync(
        string userId,
        Guid? currentSessionId,
        CancellationToken cancellationToken
    );
}
