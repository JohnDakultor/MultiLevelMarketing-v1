namespace modular_mlm.Application.Messaging.Auditing;

public sealed record DeadLetterAuditSnapshot(
    Guid MessageId,
    Guid OrganizationId,
    string MessageType,
    int Attempts,
    DateTimeOffset? DeadLetteredAt,
    DateTimeOffset NextAttemptAt,
    Guid ReplayActorUserId,
    string ReplayReason
);
