namespace modular_mlm.Application.Common.Models;

public sealed record DeadLetterMessageQuery(
    Guid OrganizationId,
    int Page,
    int PageSize,
    string? MessageType,
    DateTimeOffset? From,
    DateTimeOffset? To
);

public sealed record DeadLetterMessageRecord(
    Guid MessageId,
    string MessageType,
    DateTimeOffset OccurredAt,
    int Attempts,
    DateTimeOffset DeadLetteredAt,
    DateTimeOffset NextAttemptAt,
    string? CorrelationId,
    string? LastErrorSummary
);

public sealed record DeadLetterMessagePage(
    IReadOnlyList<DeadLetterMessageRecord> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed record DeadLetterReplayRequest(
    Guid OrganizationId,
    Guid MessageId,
    int ExpectedAttempts,
    string ActorUserId,
    string Reason,
    DateTimeOffset ReplayedAt
);

public sealed record DeadLetterReplayResult(
    Guid MessageId,
    int PreviousAttempts,
    DateTimeOffset ReplayedAt,
    DateTimeOffset NextAttemptAt
);
