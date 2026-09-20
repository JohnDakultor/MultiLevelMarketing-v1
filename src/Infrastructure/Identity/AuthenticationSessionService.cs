using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Identity;

public sealed class AuthenticationSessionService(ApplicationDbContext db)
    : IAuthenticationSessionService
{
    public async Task<Guid> CreateOrRotateSessionAsync(
        string userId,
        Guid? currentSessionId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    )
    {
        ValidateUserId(userId);
        var replacement = AuthenticationSession.Create(
            userId,
            SHA256.HashData(RandomNumberGenerator.GetBytes(32)),
            createdAt,
            expiresAt
        );

        if (currentSessionId.HasValue)
        {
            var current = await db.AuthenticationSessions.SingleOrDefaultAsync(
                session => session.Id == currentSessionId && session.UserId == userId,
                cancellationToken
            );
            if (current is null)
                throw new KeyNotFoundException("Authentication session was not found.");
            current.MarkRotated(createdAt, replacement.Id);
        }

        db.AuthenticationSessions.Add(replacement);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return replacement.Id;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The authentication session changed concurrently.");
        }
    }

    public Task<bool> IsSessionActiveAsync(
        string userId,
        Guid sessionId,
        DateTimeOffset currentTime,
        CancellationToken cancellationToken
    )
    {
        ValidateUserId(userId);
        if (sessionId == Guid.Empty)
            return Task.FromResult(false);
        return db
            .AuthenticationSessions.AsNoTracking()
            .AnyAsync(
                session =>
                    session.Id == sessionId
                    && session.UserId == userId
                    && session.RevokedAt == null
                    && session.ExpiresAt > currentTime,
                cancellationToken
            );
    }

    public async Task<bool> RevokeSessionAsync(
        string userId,
        Guid sessionId,
        DateTimeOffset revokedAt,
        string reason,
        CancellationToken cancellationToken
    )
    {
        ValidateUserId(userId);
        var session = await db.AuthenticationSessions.SingleOrDefaultAsync(
            candidate => candidate.Id == sessionId && candidate.UserId == userId,
            cancellationToken
        );
        if (session is null || session.IsRevoked)
            return false;
        session.Revoke(revokedAt, reason);
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllSessionsAsync(
        string userId,
        DateTimeOffset revokedAt,
        string reason,
        Guid? exceptSessionId,
        CancellationToken cancellationToken
    )
    {
        ValidateUserId(userId);
        var normalizedReason = NormalizeReason(reason);
        return await db
            .AuthenticationSessions.Where(session =>
                session.UserId == userId
                && session.RevokedAt == null
                && (!exceptSessionId.HasValue || session.Id != exceptSessionId.Value)
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(session => session.RevokedAt, revokedAt)
                        .SetProperty(session => session.RevocationReason, normalizedReason),
                cancellationToken
            );
    }

    public async Task<IReadOnlyList<AuthenticationSessionInfo>> GetSessionsAsync(
        string userId,
        Guid? currentSessionId,
        CancellationToken cancellationToken
    )
    {
        ValidateUserId(userId);
        return await db
            .AuthenticationSessions.AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.LastRotatedAt)
            .Select(session => new AuthenticationSessionInfo(
                session.Id,
                session.CreatedAt,
                session.LastRotatedAt,
                session.ExpiresAt,
                session.RevokedAt,
                session.RevocationReason,
                currentSessionId.HasValue && session.Id == currentSessionId.Value
            ))
            .ToListAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The authentication session changed concurrently.");
        }
    }

    private static void ValidateUserId(string userId) =>
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

    private static string NormalizeReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalized = reason.Trim();
        if (normalized.Length > AuthenticationSession.MaximumRevocationReasonLength)
            throw new ArgumentException("Revocation reason is too long.", nameof(reason));
        return normalized;
    }
}
