using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.Messaging;

public sealed class OutboxDispatcher(
    ApplicationDbContext db,
    IOutboxPublisher publisher,
    OutboxConsumerRegistry registry,
    OutboxDeadLetterAlertSender deadLetterAlerts,
    IOptions<OutboxDispatchOptions> options,
    TimeProvider clock,
    ILogger<OutboxDispatcher> logger
)
{
    private const string ConsumerName = "MediatRDomainEvents";

    public async Task<int> DispatchBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var workerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
        var candidateIds = await db
            .OutboxMessages.Where(message =>
                message.ProcessedAt == null
                && message.DeadLetteredAt == null
                && message.NextAttemptAt <= now
                && (message.ClaimedUntil == null || message.ClaimedUntil < now)
            )
            .OrderBy(message => message.OccurredAt)
            .Select(message => message.Id)
            .Take(Math.Clamp(batchSize, 1, 100))
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0)
            return 0;

        await db
            .OutboxMessages.Where(message =>
                candidateIds.Contains(message.Id)
                && (message.ClaimedUntil == null || message.ClaimedUntil < now)
            )
            .ExecuteUpdateAsync(
                updates =>
                    updates
                        .SetProperty(message => message.ClaimedBy, workerId)
                        .SetProperty(
                            message => message.ClaimedUntil,
                            now.AddSeconds(options.Value.ClaimSeconds)
                        ),
                cancellationToken
            );
        var messages = await db
            .OutboxMessages.Where(message => message.ClaimedBy == workerId)
            .OrderBy(message => message.OccurredAt)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var message in messages)
        {
            try
            {
                var dispatched = false;
                var duplicate = await db.ProcessedMessages.AnyAsync(
                    record => record.MessageId == message.Id && record.ConsumerName == ConsumerName,
                    cancellationToken
                );
                if (!duplicate)
                {
                    if (!registry.TryResolve(message.MessageType, out var messageType))
                        throw new InvalidOperationException(
                            $"Outbox message type '{message.MessageType}' is not allowed."
                        );
                    var domainEvent =
                        JsonSerializer.Deserialize(message.PayloadJson, messageType)
                        ?? throw new InvalidOperationException("Outbox payload is empty.");
                    await publisher.PublishAsync(domainEvent, cancellationToken);
                    dispatched = true;
                    db.ProcessedMessages.Add(
                        ProcessedMessage.Create(message.Id, ConsumerName, clock.GetUtcNow())
                    );
                }
                message.MarkProcessed(clock.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                if (dispatched)
                    RecordTelemetry(message);
                processed++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Outbox message {MessageId} failed", message.Id);
                var attempt = message.Attempts + 1;
                var deadLettered = message.MarkFailed(
                    exception.GetType().Name,
                    clock.GetUtcNow(),
                    options.Value.MaximumAttempts,
                    CalculateRetryDelay(attempt)
                );
                if (deadLettered)
                    await deadLetterAlerts.SendAsync(message, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        return processed;
    }

    private void RecordTelemetry(OutboxMessage message)
    {
        var now = clock.GetUtcNow();
        var tags = new TagList { { "organization.id", message.OrganizationId } };
        MarketplaceTelemetry.OutboxLag.Record(
            Math.Max(0, (now - message.OccurredAt).TotalSeconds),
            tags
        );
        var messageTypeName = message.MessageType[(message.MessageType.LastIndexOf('.') + 1)..];
        switch (messageTypeName)
        {
            case "OrderPaidEvent":
                MarketplaceTelemetry.OrdersPaid.Add(1, tags);
                MarketplaceTelemetry.PaymentSuccesses.Add(1, tags);
                MarketplaceTelemetry.CompensationLag.Record(
                    Math.Max(0, (now - message.OccurredAt).TotalSeconds),
                    tags
                );
                break;
            case "PaymentFailedEvent":
                MarketplaceTelemetry.PaymentFailures.Add(1, tags);
                break;
            case "PayoutFailedEvent":
                MarketplaceTelemetry.PayoutFailures.Add(1, tags);
                break;
        }
    }

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        var configuration = options.Value;
        var seconds = Math.Min(
            configuration.MaximumRetrySeconds,
            configuration.InitialRetrySeconds * Math.Pow(2, Math.Max(0, attempt - 1))
        );
        var jitter = seconds * configuration.JitterPercentage / 100d;
        return TimeSpan.FromSeconds(Math.Max(1, seconds + Random.Shared.NextDouble() * jitter));
    }
}
