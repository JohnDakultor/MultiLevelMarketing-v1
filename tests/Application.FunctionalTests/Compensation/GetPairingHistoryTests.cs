using modular_mlm.Application.Compensation.Queries.GetPairingHistory;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Compensation;

using static Infrastructure.TestApp;

public sealed class GetPairingHistoryTests : TestBase
{
    [Test]
    public async Task ShouldReturnOnlyTheAgentsOverlappingRunsInNewestFirstOrder()
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "Pairing History",
            $"pairing-history-{Guid.NewGuid():N}",
            "PHP",
            "Asia/Manila",
            "en-PH"
        );
        var otherOrganization = Organization.Create(
            "Other Pairing History",
            $"other-pairing-history-{Guid.NewGuid():N}",
            "PHP",
            "Asia/Manila",
            "en-PH"
        );
        var agent = CreateActiveAgent(organization.Id, "PRIMARY");
        var otherAgent = CreateActiveAgent(organization.Id, "OTHER");
        var crossTenantAgent = CreateActiveAgent(otherOrganization.Id, "CROSS-TENANT");
        var planId = Guid.NewGuid();
        var older = CreateCompletedRun(
            organization.Id,
            agent.Id,
            planId,
            now.AddDays(-20),
            now.AddDays(-15),
            "older"
        );
        var newer = CreateCompletedRun(
            organization.Id,
            agent.Id,
            planId,
            now.AddDays(-10),
            now.AddDays(-5),
            "newer"
        );
        var outsideWindow = CreateCompletedRun(
            organization.Id,
            agent.Id,
            planId,
            now.AddDays(-60),
            now.AddDays(-50),
            "outside-window"
        );
        var otherAgentsRun = CreateCompletedRun(
            organization.Id,
            otherAgent.Id,
            planId,
            now.AddDays(-9),
            now.AddDays(-4),
            "other-agent"
        );
        var crossTenantRun = CreateCompletedRun(
            otherOrganization.Id,
            crossTenantAgent.Id,
            Guid.NewGuid(),
            now.AddDays(-8),
            now.AddDays(-3),
            "cross-tenant"
        );

        await AddAsync(organization);
        await AddAsync(otherOrganization);
        await AddAsync(agent);
        await AddAsync(otherAgent);
        await AddAsync(crossTenantAgent);
        await AddAsync(older);
        await AddAsync(newer);
        await AddAsync(outsideWindow);
        await AddAsync(otherAgentsRun);
        await AddAsync(crossTenantRun);
        await RunAsAdministratorAsync(organization.Id);

        var result = await SendAsync(
            new GetPairingHistoryQuery(organization.Id, agent.Id, now.AddDays(-30), now)
        );

        result.Count.ShouldBe(2);
        result[0].PeriodEnd.ShouldBe(newer.PeriodEnd, TimeSpan.FromMilliseconds(1));
        result[1].PeriodEnd.ShouldBe(older.PeriodEnd, TimeSpan.FromMilliseconds(1));
        result[0].PeriodEnd.ShouldBeGreaterThan(result[1].PeriodEnd);
        result.ShouldAllBe(run => run.OrganizationId == organization.Id);
        result.ShouldAllBe(run => run.AgentId == agent.Id);
    }

    [Test]
    public async Task ShouldProjectTheCompletePairingAuditSnapshot()
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "Pairing Projection",
            $"pairing-projection-{Guid.NewGuid():N}",
            "PHP"
        );
        var agent = CreateActiveAgent(organization.Id, "PROJECTION");
        var planId = Guid.NewGuid();
        var run = CreateCompletedRun(
            organization.Id,
            agent.Id,
            planId,
            now.AddDays(-7),
            now,
            "projection"
        );

        await AddAsync(organization);
        await AddAsync(agent);
        await AddAsync(run);
        await RunAsAdministratorAsync(organization.Id);

        var result = await SendAsync(
            new GetPairingHistoryQuery(
                organization.Id,
                agent.Id,
                now.AddDays(-8),
                now.AddMinutes(1)
            )
        );

        var item = result.ShouldHaveSingleItem();
        item.CommissionPlanId.ShouldBe(planId);
        item.CommissionPlanVersion.ShouldBe(1);
        item.QualificationPassed.ShouldBeTrue();
        item.QualificationFailureReason.ShouldBeNull();
        item.LeftBefore.ShouldBe(1_000m);
        item.RightBefore.ShouldBe(600m);
        item.MatchedVolume.ShouldBe(600m);
        item.LeftConsumed.ShouldBe(600m);
        item.RightConsumed.ShouldBe(600m);
        item.LeftAfter.ShouldBe(400m);
        item.RightAfter.ShouldBe(0m);
        item.GrossCommission.ShouldBe(120m);
        item.CappedAmount.ShouldBe(20m);
        item.NetCommission.ShouldBe(100m);
        item.CapApplied.ShouldBeTrue();
        item.Status.ShouldBe(BinaryPairingRunStatus.Completed);
        item.ProcessedAt.ShouldNotBeNull();
        item.ProcessedAt.Value.ShouldBe(run.ProcessedAt!.Value, TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task ShouldRejectAnAgentFromAnotherOrganization()
    {
        var organization = Organization.Create(
            "Requested Organization",
            $"requested-organization-{Guid.NewGuid():N}",
            "PHP"
        );
        var otherOrganization = Organization.Create(
            "Agents Organization",
            $"agents-organization-{Guid.NewGuid():N}",
            "PHP"
        );
        var agent = CreateActiveAgent(otherOrganization.Id, "WRONG-ORGANIZATION");
        await AddAsync(organization);
        await AddAsync(otherOrganization);
        await AddAsync(agent);
        await RunAsAdministratorAsync(organization.Id);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            SendAsync(
                new GetPairingHistoryQuery(
                    organization.Id,
                    agent.Id,
                    DateTimeOffset.UtcNow.AddDays(-7),
                    DateTimeOffset.UtcNow
                )
            )
        );
    }

    private static BinaryPairingRun CreateCompletedRun(
        Guid organizationId,
        Guid agentId,
        Guid planId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string keySuffix
    )
    {
        var run = BinaryPairingRun.Create(
            organizationId,
            agentId,
            planId,
            1,
            periodStart,
            periodEnd,
            $"{organizationId:N}:{agentId:N}:{keySuffix}"
        );
        run.Complete(1_000m, 600m, 600m, 600m, 600m, 120m, 20m, periodEnd);
        return run;
    }

    private static Agent CreateActiveAgent(Guid organizationId, string code)
    {
        var uniqueCode = $"{code}-{Guid.NewGuid():N}";
        var agent = Agent.Apply(
            organizationId,
            Guid.NewGuid().ToString(),
            uniqueCode,
            $"REF-{uniqueCode}",
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        return agent;
    }
}
