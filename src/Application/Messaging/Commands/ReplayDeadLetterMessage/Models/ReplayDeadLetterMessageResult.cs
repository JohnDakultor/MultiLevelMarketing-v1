namespace modular_mlm.Application.Messaging.Commands.ReplayDeadLetterMessage.Models;

public sealed record ReplayDeadLetterMessageResult(
    Guid MessageId,
    int PreviousAttempts,
    DateTimeOffset RequeuedAt,
    DateTimeOffset NextAttemptAt
);
