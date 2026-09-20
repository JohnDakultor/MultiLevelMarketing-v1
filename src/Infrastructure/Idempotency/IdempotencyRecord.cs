using modular_mlm.Domain.Idempotency;

namespace modular_mlm.Infrastructure.Idempotency;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord() { }

    public Guid Id { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Scope { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public IdempotencyStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int? StatusCode { get; private set; }
    public string? ProtectedOutcome { get; private set; }
    public long Version { get; private set; }

    public static IdempotencyRecord Start(
        Guid? organizationId,
        string scope,
        string keyHash,
        string requestHash,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt
    )
    {
        ValidateIdentity(scope, keyHash, requestHash);
        if (expiresAt <= startedAt)
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "Expiration must be later than the start time."
            );

        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Scope = scope,
            KeyHash = keyHash,
            RequestHash = requestHash,
            Status = IdempotencyStatus.Processing,
            StartedAt = startedAt,
            ExpiresAt = expiresAt,
        };
    }

    public void Complete(
        string requestHash,
        int statusCode,
        string? protectedOutcome,
        DateTimeOffset completedAt,
        DateTimeOffset expiresAt
    )
    {
        RequireMatchingRequest(requestHash);
        if (Status == IdempotencyStatus.Completed)
            return;
        if (Status != IdempotencyStatus.Processing)
            throw new InvalidOperationException("Only a processing request can be completed.");
        if (statusCode is < 100 or > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode));
        if (completedAt < StartedAt)
            throw new ArgumentOutOfRangeException(nameof(completedAt));
        if (expiresAt <= completedAt)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));

        Status = IdempotencyStatus.Completed;
        StatusCode = statusCode;
        ProtectedOutcome = protectedOutcome;
        CompletedAt = completedAt;
        ExpiresAt = expiresAt;
        Version++;
    }

    public void MarkFailedForRetry(
        string requestHash,
        DateTimeOffset failedAt,
        DateTimeOffset retryAt
    )
    {
        RequireMatchingRequest(requestHash);
        if (Status == IdempotencyStatus.Completed)
            return;
        if (retryAt <= failedAt)
            throw new ArgumentOutOfRangeException(
                nameof(retryAt),
                "Retry time must be later than the failure time."
            );

        Status = IdempotencyStatus.Failed;
        CompletedAt = null;
        StatusCode = null;
        ProtectedOutcome = null;
        ExpiresAt = retryAt;
        Version++;
    }

    public void Restart(string requestHash, DateTimeOffset startedAt, DateTimeOffset expiresAt)
    {
        if (expiresAt <= startedAt)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));

        RequestHash = Require(requestHash, nameof(requestHash));
        Status = IdempotencyStatus.Processing;
        StartedAt = startedAt;
        ExpiresAt = expiresAt;
        CompletedAt = null;
        StatusCode = null;
        ProtectedOutcome = null;
        Version++;
    }

    private void RequireMatchingRequest(string requestHash)
    {
        if (!string.Equals(RequestHash, requestHash, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The idempotency key belongs to a different request payload."
            );
    }

    private static void ValidateIdentity(string scope, string keyHash, string requestHash)
    {
        Require(scope, nameof(scope));
        Require(keyHash, nameof(keyHash));
        Require(requestHash, nameof(requestHash));
    }

    private static string Require(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value;
}
