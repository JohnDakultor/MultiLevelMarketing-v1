using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using modular_mlm.Infrastructure.Monitoring;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Application.FunctionalTests.Monitoring;

public sealed class PhaseElevenMonitoringTests : TestBase
{
    [Test]
    public async Task LivenessEndpointIsAvailableWithoutExternalDependencies()
    {
        using var client = FunctionalTestSetup.CreateClient();

        using var response = await client.GetAsync("/alive");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task DatabaseAndWorkerBacklogReadinessUsePostgreSql()
    {
        await TestApp.ExecuteInScopeAsync(async services =>
        {
            var database = services.GetRequiredService<DatabaseReadinessHealthCheck>();
            var backlog = services.GetRequiredService<WorkerBacklogHealthCheck>();

            (await database.CheckHealthAsync(new HealthCheckContext())).Status.ShouldBe(
                HealthStatus.Healthy
            );
            (await backlog.CheckHealthAsync(new HealthCheckContext())).Status.ShouldBe(
                HealthStatus.Healthy
            );
            return true;
        });
    }

    [Test]
    public void MarketplaceTelemetryDefinesConfiguredMeterAndActivitySource()
    {
        MarketplaceTelemetry.Meter.Name.ShouldBe(MarketplaceTelemetry.ActivitySourceName);
        MarketplaceTelemetry.ActivitySource.Name.ShouldBe(MarketplaceTelemetry.ActivitySourceName);
    }
}
