using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Notifications;

public sealed record NotificationDeliveryJobResult(
    int Claimed,
    int Delivered,
    int Retrying,
    int DeadLettered,
    TimeSpan Duration
);

public sealed class NotificationDeliveryJob(
    ApplicationDbContext db,
    INotificationSender sender,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<NotificationDeliveryOptions> options,
    TimeProvider clock,
    ILogger<NotificationDeliveryJob> logger
)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        NotificationDeliveryEnvelope.ProtectionPurpose
    );

    public async Task<NotificationDeliveryJobResult> ExecuteAsync(
        CancellationToken cancellationToken
    )
    {
        var started = Stopwatch.GetTimestamp();
        var configuration = options.Value;
        var now = clock.GetUtcNow();
        var workerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
        var candidateIds = await db
            .NotificationDeliveryEnvelopes.AsNoTracking()
            .Where(envelope =>
                envelope.ReadyAt != null
                && envelope.ReadyAt <= now
                && envelope.NextAttemptAt <= now
                && envelope.DeliveredAt == null
                && envelope.DeadLetteredAt == null
                && (envelope.ClaimedUntil == null || envelope.ClaimedUntil < now)
            )
            .OrderBy(envelope => envelope.NextAttemptAt)
            .ThenBy(envelope => envelope.Id)
            .Select(envelope => envelope.Id)
            .Take(configuration.BatchSize)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count > 0)
            await db
                .NotificationDeliveryEnvelopes.Where(envelope =>
                    candidateIds.Contains(envelope.Id)
                    && envelope.DeliveredAt == null
                    && envelope.DeadLetteredAt == null
                    && (envelope.ClaimedUntil == null || envelope.ClaimedUntil < now)
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(envelope => envelope.ClaimedBy, workerId)
                            .SetProperty(
                                envelope => envelope.ClaimedUntil,
                                now.AddSeconds(configuration.ClaimSeconds)
                            ),
                    cancellationToken
                );

        var envelopes = await db
            .NotificationDeliveryEnvelopes.Where(envelope => envelope.ClaimedBy == workerId)
            .OrderBy(envelope => envelope.NextAttemptAt)
            .ToListAsync(cancellationToken);
        var delivered = 0;
        var retrying = 0;
        var deadLettered = 0;

        foreach (var envelope in envelopes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (envelope.ExpiresAt <= now)
            {
                envelope.Expire(now);
                deadLettered++;
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            NotificationDeliveryResult result;
            try
            {
                var payloadJson = _protector.Unprotect(envelope.ProtectedPayload);
                var payload =
                    JsonSerializer.Deserialize<NotificationDeliveryPayload>(payloadJson)
                    ?? throw new JsonException("Delivery payload is empty.");
                result = await sender.SendAsync(
                    new NotificationDeliveryRequest(
                        envelope.NotificationId ?? envelope.Id,
                        envelope.OrganizationId,
                        payload.RecipientUserId,
                        payload.RecipientAddress,
                        envelope.Channel,
                        envelope.TemplateKey,
                        payload.Culture,
                        payload.Variables
                    ),
                    cancellationToken
                );
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    "Notification envelope {EnvelopeId} contains an unreadable protected payload: {ErrorType}",
                    envelope.Id,
                    exception.GetType().Name
                );
                result = NotificationDeliveryResult.PermanentFailure("INVALID_PROTECTED_PAYLOAD");
            }

            if (result.Succeeded)
            {
                envelope.MarkDelivered(clock.GetUtcNow());
                if (envelope.NotificationId is { } notificationId)
                {
                    var notification = await db.Notifications.SingleOrDefaultAsync(
                        item =>
                            item.Id == notificationId
                            && item.OrganizationId == envelope.OrganizationId,
                        cancellationToken
                    );
                    notification?.MarkDelivered(clock.GetUtcNow());
                }
                delivered++;
            }
            else
            {
                var attempt = envelope.AttemptCount + 1;
                var delay = result.RetryAfter ?? CalculateDelay(configuration, attempt);
                var permanent = !result.IsTransient;
                envelope.MarkFailed(
                    result.FailureCode!,
                    permanent,
                    clock.GetUtcNow(),
                    configuration.MaximumAttempts,
                    delay
                );
                if (envelope.NotificationId is { } notificationId)
                {
                    var notification = await db.Notifications.SingleOrDefaultAsync(
                        item =>
                            item.Id == notificationId
                            && item.OrganizationId == envelope.OrganizationId,
                        cancellationToken
                    );
                    notification?.MarkDeliveryFailed(
                        result.FailureCode!,
                        attempt,
                        permanent || attempt >= configuration.MaximumAttempts,
                        clock.GetUtcNow()
                    );
                }
                if (permanent || attempt >= configuration.MaximumAttempts)
                    deadLettered++;
                else
                    retrying++;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        return new NotificationDeliveryJobResult(
            envelopes.Count,
            delivered,
            retrying,
            deadLettered,
            Stopwatch.GetElapsedTime(started)
        );
    }

    private static TimeSpan CalculateDelay(
        NotificationDeliveryOptions configuration,
        int attempt
    ) =>
        TimeSpan.FromSeconds(
            Math.Min(
                configuration.MaximumRetrySeconds,
                configuration.InitialRetrySeconds * Math.Pow(2, Math.Max(0, attempt - 1))
            )
        );
}
