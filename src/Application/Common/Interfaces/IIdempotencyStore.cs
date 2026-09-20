using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IIdempotencyStore
{
    Task<IdempotencyDecision> TryBeginAsync(
        Guid? organizationId,
        string scope,
        string key,
        string requestHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    );

    Task CompleteAsync(
        Guid? organizationId,
        string scope,
        string key,
        string requestHash,
        int statusCode,
        string? outcome,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken
    );

    Task FailAsync(
        Guid? organizationId,
        string scope,
        string key,
        string requestHash,
        DateTimeOffset failedAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    );

    Task<IdempotencyDecision?> GetAsync(
        Guid? organizationId,
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken
    );
}
