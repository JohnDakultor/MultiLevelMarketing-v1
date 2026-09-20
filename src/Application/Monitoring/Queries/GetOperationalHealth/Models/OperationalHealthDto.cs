namespace modular_mlm.Application.Monitoring.Queries.GetOperationalHealth.Models;

public sealed record OperationalHealthDto(
    Guid OrganizationId,
    DateTimeOffset ObservedAt,
    int PendingOutboxMessages,
    double? OldestPendingOutboxAgeSeconds,
    int FailedWebhookAttempts,
    int PaymentsAwaitingReconciliation,
    int PayoutsAwaitingProviderCompletion,
    int CompensationBacklog,
    int RecentPairingFailures,
    double? LatestPairingDurationMilliseconds
);
