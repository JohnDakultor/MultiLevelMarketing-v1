namespace modular_mlm.Application.Common.Models;

public enum IdempotencyDecisionKind
{
    Started = 1,
    InProgress = 2,
    Completed = 3,
    Conflict = 4,
}

public sealed record IdempotencyDecision
{
    private const int MaximumOutcomeLength = 32_768;

    private IdempotencyDecision(
        IdempotencyDecisionKind kind,
        DateTimeOffset expiresAt,
        int? statusCode,
        string? outcome
    )
    {
        if (statusCode is < 100 or > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode));
        if (outcome?.Length > MaximumOutcomeLength)
            throw new ArgumentException(
                $"Stored idempotency outcomes cannot exceed {MaximumOutcomeLength} characters.",
                nameof(outcome)
            );
        if (kind == IdempotencyDecisionKind.Completed && statusCode is null)
            throw new ArgumentException("A completed decision requires a status code.");
        if (
            kind != IdempotencyDecisionKind.Completed
            && (statusCode is not null || outcome is not null)
        )
            throw new ArgumentException("Only completed decisions may contain a stored outcome.");

        Kind = kind;
        ExpiresAt = expiresAt;
        StatusCode = statusCode;
        Outcome = outcome;
    }

    public IdempotencyDecisionKind Kind { get; }
    public DateTimeOffset ExpiresAt { get; }
    public int? StatusCode { get; }
    public string? Outcome { get; }

    public static IdempotencyDecision Started(DateTimeOffset expiresAt) =>
        new(IdempotencyDecisionKind.Started, expiresAt, null, null);

    public static IdempotencyDecision InProgress(DateTimeOffset expiresAt) =>
        new(IdempotencyDecisionKind.InProgress, expiresAt, null, null);

    public static IdempotencyDecision Completed(
        int statusCode,
        string? outcome,
        DateTimeOffset expiresAt
    ) => new(IdempotencyDecisionKind.Completed, expiresAt, statusCode, outcome);

    public static IdempotencyDecision Conflict(DateTimeOffset expiresAt) =>
        new(IdempotencyDecisionKind.Conflict, expiresAt, null, null);
}
