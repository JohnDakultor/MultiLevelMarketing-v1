namespace modular_mlm.Infrastructure.Identity;

public sealed class AuthenticationSession
{
    public const int TokenFamilyHashLength = 32;
    public const int MaximumRevocationReasonLength = 200;

    private AuthenticationSession() { }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public byte[] TokenFamilyHash { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastRotatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }
    public uint Version { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsActiveAt(DateTimeOffset currentTime) => !IsRevoked && currentTime < ExpiresAt;

    public bool IsExpired(DateTimeOffset currentTime) => currentTime >= ExpiresAt;

    public static AuthenticationSession Create(
        string userId,
        byte[] tokenFamilyHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(tokenFamilyHash);
        if (tokenFamilyHash.Length != TokenFamilyHashLength)
            throw new ArgumentException(
                "Token family hash must be 32 bytes.",
                nameof(tokenFamilyHash)
            );
        if (expiresAt <= createdAt)
            throw new ArgumentException("Expiration must follow creation.", nameof(expiresAt));

        return new AuthenticationSession
        {
            Id = Guid.NewGuid(),
            UserId = userId.Trim(),
            TokenFamilyHash = [.. tokenFamilyHash],
            CreatedAt = createdAt,
            LastRotatedAt = createdAt,
            ExpiresAt = expiresAt,
        };
    }

    public void MarkRotated(DateTimeOffset rotatedAt, Guid replacementSessionId)
    {
        if (replacementSessionId == Guid.Empty || replacementSessionId == Id)
            throw new ArgumentException(
                "A distinct replacement session is required.",
                nameof(replacementSessionId)
            );
        if (!IsActiveAt(rotatedAt))
            throw new InvalidOperationException("Only an active session can be rotated.");
        LastRotatedAt = rotatedAt;
        Revoke(rotatedAt, "rotated", replacementSessionId);
    }

    public void Revoke(
        DateTimeOffset revokedAt,
        string? reason = null,
        Guid? replacedBySessionId = null
    )
    {
        if (IsRevoked)
            return;
        if (revokedAt < CreatedAt)
            throw new ArgumentOutOfRangeException(nameof(revokedAt));
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "revoked" : reason.Trim();
        if (normalizedReason.Length > MaximumRevocationReasonLength)
            throw new ArgumentException("Revocation reason is too long.", nameof(reason));
        RevokedAt = revokedAt;
        RevocationReason = normalizedReason;
        ReplacedBySessionId = replacedBySessionId;
    }
}
