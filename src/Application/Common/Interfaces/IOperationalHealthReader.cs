namespace modular_mlm.Application.Common.Interfaces;

public sealed record OperationalHealthSnapshot(
    int PendingOutboxMessages,
    double? OldestPendingOutboxAgeSeconds,
    int FailedWebhookAttempts,
    int PaymentsAwaitingReconciliation,
    int PayoutsAwaitingProviderCompletion,
    int CompensationBacklog,
    int RecentPairingFailures,
    double? LatestPairingDurationMilliseconds
);

public interface IOperationalHealthReader
{
    Task<OperationalHealthSnapshot> ReadAsync(
        Guid organizationId,
        DateTimeOffset now,
        CancellationToken cancellationToken
    );
}
