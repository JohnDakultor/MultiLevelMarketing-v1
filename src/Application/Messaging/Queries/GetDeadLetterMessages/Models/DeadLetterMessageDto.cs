namespace modular_mlm.Application.Messaging.Queries.GetDeadLetterMessages.Models;

public sealed record DeadLetterMessageDto(
    Guid MessageId,
    string MessageType,
    DateTimeOffset OccurredAt,
    int Attempts,
    DateTimeOffset DeadLetteredAt,
    DateTimeOffset NextAttemptAt,
    string? CorrelationId,
    string? LastErrorSummary
);
