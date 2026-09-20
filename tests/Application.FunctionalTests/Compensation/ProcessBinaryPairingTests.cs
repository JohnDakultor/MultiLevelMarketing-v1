using modular_mlm.Application.Compensation.Commands.ProcessBinaryPairing;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Compensation;

using static Infrastructure.TestApp;

public sealed class ProcessBinaryPairingTests : TestBase
{
    [Test]
    public async Task ShouldProcessPairingAtomicallyAndBeIdempotent()
    {
        var data = await SeedAsync(1_000m, 600m);
        var command = Command(data, "successful-run");

        var runId = await SendAsync(command);
        var repeatedRunId = await SendAsync(command);

        repeatedRunId.ShouldBe(runId);
        var run = await SingleAsync<BinaryPairingRun>(candidate => candidate.Id == runId);
        run.Status.ShouldBe(BinaryPairingRunStatus.Completed);
        run.LeftBefore.ShouldBe(1_000m);
        run.RightBefore.ShouldBe(600m);
        run.MatchedVolume.ShouldBe(600m);
        run.LeftAfter.ShouldBe(400m);
        run.RightAfter.ShouldBe(0m);
        run.GrossCommission.ShouldBe(60m);
        run.NetCommission.ShouldBe(60m);

        var balance = await SingleAsync<BinaryVolumeBalance>(candidate =>
            candidate.AgentId == data.Agent.Id
        );
        balance.LeftAvailable.ShouldBe(400m);
        balance.RightAvailable.ShouldBe(0m);
        (await CountAsync<BinaryPairingRun>()).ShouldBe(1);
        (await CountAsync<BinaryVolumeEntry>()).ShouldBe(2);
        (await CountAsync<CommissionTransaction>()).ShouldBe(1);
        (await CountAsync<WalletEntry>()).ShouldBe(1);

        var commission = await SingleAsync<CommissionTransaction>(candidate =>
            candidate.PairingRunId == runId
        );
        commission.SourceOrderId.ShouldBeNull();
        commission.Amount.ShouldBe(60m);
        commission.Type.ShouldBe(CommissionType.BinaryPairing);
    }

    [Test]
    public async Task ShouldApplyTheCapAndPreserveUnpaidVolume()
    {
        var data = await SeedAsync(
            1_000m,
            1_000m,
            capRulesJson: """
            { "enabled": true, "maximumAmount": 50 }
            """
        );

        var runId = await SendAsync(Command(data, "capped-run"));

        var run = await SingleAsync<BinaryPairingRun>(candidate => candidate.Id == runId);
        run.GrossCommission.ShouldBe(100m);
        run.CappedAmount.ShouldBe(50m);
        run.NetCommission.ShouldBe(50m);
        run.LeftConsumed.ShouldBe(500m);
        run.RightConsumed.ShouldBe(500m);
        var balance = await SingleAsync<BinaryVolumeBalance>(candidate =>
            candidate.AgentId == data.Agent.Id
        );
        balance.LeftAvailable.ShouldBe(500m);
        balance.RightAvailable.ShouldBe(500m);
    }

    [Test]
    public async Task ShouldRecordAQualificationFailureWithoutConsumingVolume()
    {
        var data = await SeedAsync(
            500m,
            500m,
            activateAgent: false,
            qualificationRulesJson: """
            { "requireActiveAgent": true }
            """
        );

        var runId = await SendAsync(Command(data, "unqualified-run"));

        var run = await SingleAsync<BinaryPairingRun>(candidate => candidate.Id == runId);
        run.Status.ShouldBe(BinaryPairingRunStatus.Skipped);
        run.QualificationPassed.ShouldBeFalse();
        (run.QualificationFailureReason ?? string.Empty).ShouldContain("active");
        (await CountAsync<CommissionTransaction>()).ShouldBe(0);
        (await CountAsync<BinaryVolumeEntry>()).ShouldBe(0);
        var balance = await SingleAsync<BinaryVolumeBalance>(candidate =>
            candidate.AgentId == data.Agent.Id
        );
        balance.LeftAvailable.ShouldBe(500m);
        balance.RightAvailable.ShouldBe(500m);
    }

    private static ProcessBinaryPairingCommand Command(PairingData data, string key) =>
        new(
            data.Organization.Id,
            data.Agent.Id,
            data.Plan.Id,
            data.PeriodStart,
            data.PeriodEnd,
            $"{data.Organization.Id:N}:{key}"
        );

    private static async Task<PairingData> SeedAsync(
        decimal left,
        decimal right,
        bool activateAgent = true,
        string? qualificationRulesJson = null,
        string? capRulesJson = null
    )
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "Pairing Processing",
            $"pairing-processing-{Guid.NewGuid():N}",
            "PHP",
            "Asia/Manila",
            "en-PH"
        );
        var agent = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            $"AG-{Guid.NewGuid():N}",
            $"REF-{Guid.NewGuid():N}",
            now.AddDays(-30)
        );
        if (activateAgent)
            agent.Activate(now.AddDays(-29));
        var plan = CommissionPlan.Draft(organization.Id, "Binary", 1, now.AddDays(-30));
        plan.ConfigureBinaryPairing(
            BinaryPairingRule.Percentage(0.10m, ProcessingFrequency.Weekly)
        );
        if (qualificationRulesJson is not null)
            plan.ConfigureQualificationRules(qualificationRulesJson);
        if (capRulesJson is not null)
            plan.ConfigureCapRules(capRulesJson);
        plan.Publish();
        var balance = BinaryVolumeBalance.Open(organization.Id, agent.Id);
        balance.Credit(PlacementSide.Left, left);
        balance.Credit(PlacementSide.Right, right);

        await AddAsync(organization);
        await AddAsync(agent);
        await AddAsync(plan);
        await AddAsync(balance);
        return new PairingData(organization, agent, plan, now.AddDays(-7), now);
    }

    private sealed record PairingData(
        Organization Organization,
        Agent Agent,
        CommissionPlan Plan,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd
    );
}
