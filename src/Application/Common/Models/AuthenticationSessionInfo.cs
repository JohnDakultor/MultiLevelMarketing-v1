namespace modular_mlm.Application.Common.Models;

public sealed record AuthenticationSessionInfo(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastRotatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? RevocationReason,
    bool IsCurrent
);
