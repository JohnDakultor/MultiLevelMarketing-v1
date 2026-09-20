using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Monitoring;

public sealed class WorkerBacklogHealthCheck(
    ApplicationDbContext db,
    IOptions<MonitoringOptions> options
) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var pending = await db.OutboxMessages.CountAsync(
            x => x.ProcessedAt == null && x.DeadLetteredAt == null,
            cancellationToken
        );
        var deadLetters = await db.OutboxMessages.CountAsync(
            x => x.DeadLetteredAt != null,
            cancellationToken
        );
        var data = new Dictionary<string, object>
        {
            ["pendingOutbox"] = pending,
            ["deadLetters"] = deadLetters,
        };
        if (pending >= options.Value.OutboxUnhealthyCount)
            return HealthCheckResult.Unhealthy(
                "Outbox backlog is above its critical threshold.",
                data: data
            );
        if (
            pending >= options.Value.OutboxWarningCount
            || deadLetters >= options.Value.DeadLetterWarningCount
        )
            return HealthCheckResult.Degraded(
                "Background processing requires attention.",
                data: data
            );
        return HealthCheckResult.Healthy("Worker backlogs are within thresholds.", data);
    }
}
