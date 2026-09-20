using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Reporting.Queries.GetAdminDashboard;
using modular_mlm.Application.Reporting.Queries.GetAdminReport;
using modular_mlm.Application.Reporting.Queries.GetAgentReport;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Reporting;

public sealed class ReportingTests : TestBase
{
    [Test]
    public async Task AdministratorReportUsesTenantScopedDatabaseAggregates()
    {
        var organization = Organization.Create(
            "Reporting Organization",
            $"reporting-{Guid.NewGuid():N}",
            "PHP"
        );
        await TestApp.AddAsync(organization);
        await TestApp.RunAsAdministratorAsync(organization.Id);

        var result = await TestApp.SendAsync(
            new GetAdminReportQuery(
                organization.Id,
                DateTimeOffset.UtcNow.AddDays(-30),
                DateTimeOffset.UtcNow.AddDays(1)
            )
        );

        result.OrganizationId.ShouldBe(organization.Id);
        result.Currency.ShouldBe("PHP");
        result.OrderCount.ShouldBe(0);
        result.GrossSales.ShouldBe(0m);
        result.SalesByProduct.ShouldBeEmpty();
    }

    [Test]
    public async Task AgentReportDerivesAgentFromCurrentIdentity()
    {
        var organization = Organization.Create(
            "Agent Reporting",
            $"agent-reporting-{Guid.NewGuid():N}",
            "PHP"
        );
        await TestApp.AddAsync(organization);
        var userId = await TestApp.RunAsUserAsync(
            $"report-agent-{Guid.NewGuid():N}@local",
            "Testing1234!",
            [Roles.Agent]
        );
        var agent = Agent.Apply(
            organization.Id,
            userId,
            $"A{Guid.NewGuid():N}"[..12],
            $"R{Guid.NewGuid():N}"[..12],
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        await TestApp.AddAsync(agent);

        var result = await TestApp.SendAsync(
            new GetAgentReportQuery(
                organization.Id,
                DateTimeOffset.UtcNow.AddDays(-30),
                DateTimeOffset.UtcNow.AddDays(1)
            )
        );

        result.AgentId.ShouldBe(agent.Id);
        result.AttributedOrders.ShouldBe(0);
        result.Commissions.ShouldBeEmpty();
    }

    [Test]
    public async Task DashboardReturnsActionablePendingWork()
    {
        var organization = Organization.Create(
            "Dashboard Organization",
            $"dashboard-{Guid.NewGuid():N}",
            "PHP"
        );
        await TestApp.AddAsync(organization);
        await TestApp.RunAsAdministratorAsync(organization.Id);
        var pending = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            $"A{Guid.NewGuid():N}"[..12],
            $"R{Guid.NewGuid():N}"[..12],
            DateTimeOffset.UtcNow
        );
        await TestApp.AddAsync(pending);

        var result = await TestApp.SendAsync(new GetAdminDashboardQuery(organization.Id));

        result.Tasks.ShouldContain(x => x.Code == "agent_applications" && x.Count == 1);
    }

    [Test]
    public async Task AdministratorCannotReadAnotherOrganizationsReport()
    {
        var owned = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"foreign-{Guid.NewGuid():N}", "PHP");
        await TestApp.AddAsync(owned);
        await TestApp.AddAsync(foreign);
        await TestApp.RunAsAdministratorAsync(owned.Id);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            TestApp.SendAsync(
                new GetAdminReportQuery(
                    foreign.Id,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(1)
                )
            )
        );
    }
}
