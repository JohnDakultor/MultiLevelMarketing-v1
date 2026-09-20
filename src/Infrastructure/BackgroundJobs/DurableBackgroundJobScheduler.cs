using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Payouts.Models;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class DurableBackgroundJobScheduler(ApplicationDbContext db, TimeProvider clock)
    : IBackgroundJobScheduler
{
    public async Task EnqueueAsync(
        string jobName,
        object payload,
        CancellationToken cancellationToken
    )
    {
        var normalizedName = DurableJobRegistry.Normalize(jobName);
        var (organizationId, idempotencyKey) = payload switch
        {
            ProcessPayoutJobPayload payout => (payout.OrganizationId, payout.IdempotencyKey),
            _ => throw new ArgumentException(
                $"Payload type '{payload.GetType().Name}' is not allowed for durable jobs.",
                nameof(payload)
            ),
        };
        var json = JsonSerializer.Serialize(payload, payload.GetType());
        var existing = await db.DurableBackgroundJobs.SingleOrDefaultAsync(
            job =>
                job.OrganizationId == organizationId
                && job.JobName == normalizedName
                && job.IdempotencyKey == idempotencyKey,
            cancellationToken
        );
        if (existing is not null)
        {
            if (!string.Equals(existing.PayloadJson, json, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The durable job idempotency key has different semantics."
                );
            return;
        }

        var now = clock.GetUtcNow();
        db.DurableBackgroundJobs.Add(
            DurableBackgroundJob.Create(
                organizationId,
                normalizedName,
                json,
                idempotencyKey,
                Guid.NewGuid(),
                now
            )
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
