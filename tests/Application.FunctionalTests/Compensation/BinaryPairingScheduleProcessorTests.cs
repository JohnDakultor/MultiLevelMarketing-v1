using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.BackgroundJobs;

namespace modular_mlm.Application.FunctionalTests.Compensation;

using static Infrastructure.TestApp;

public sealed class BinaryPairingScheduleProcessorTests : TestBase
{
    [Test]
    public async Task ShouldNotProcessTheSameAgentAndPeriodTwice()
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "Scheduled Pairing",
            $"scheduled-pairing-{Guid.NewGuid():N}",
            "PHP",
            "UTC"
        );
        var agent = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            $"AG-{Guid.NewGuid():N}",
            $"REF-{Guid.NewGuid():N}",
            now.AddDays(-30)
        );
        agent.Activate(now.AddDays(-29));
        var plan = CommissionPlan.Draft(organization.Id, "Scheduled Binary", 1, now.AddDays(-30));
        plan.ConfigureBinaryPairing(BinaryPairingRule.Percentage(0.10m, ProcessingFrequency.Daily));
        plan.Publish();
        var balance = BinaryVolumeBalance.Open(organization.Id, agent.Id);
        balance.Credit(PlacementSide.Left, 500m);
        balance.Credit(PlacementSide.Right, 500m);

        await AddAsync(organization);
        await AddAsync(agent);
        await AddAsync(plan);
        await AddAsync(balance);

        await ProcessSchedulesAsync();
        await ProcessSchedulesAsync();

        (await CountAsync<BinaryPairingRun>()).ShouldBe(1);
        (await CountAsync<CommissionTransaction>()).ShouldBe(1);
        var updatedBalance = await SingleAsync<BinaryVolumeBalance>(candidate =>
            candidate.AgentId == agent.Id
        );
        updatedBalance.LeftAvailable.ShouldBe(0m);
        updatedBalance.RightAvailable.ShouldBe(0m);
    }

    private static async Task ProcessSchedulesAsync()
    {
        await using var scope = FunctionalTestSetup.ScopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<BinaryPairingScheduleProcessor>();
        await processor.ProcessDueSchedulesAsync(CancellationToken.None);
    }
}
