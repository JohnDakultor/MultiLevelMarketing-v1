using Microsoft.Extensions.Diagnostics.HealthChecks;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Monitoring;

public sealed class DatabaseReadinessHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL rejected the readiness probe.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL readiness probe failed.", exception);
        }
    }
}
