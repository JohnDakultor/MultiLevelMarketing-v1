using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Network.Queries.GetAdminAgents;
using modular_mlm.Application.Network.Queries.GetAgentApplications;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Network;

using static Infrastructure.TestApp;

public sealed class AgentApplicationQueueTests : TestBase
{
    [Test]
    public async Task AdministratorCanFilterAndPageOwnOrganizationAgents()
    {
        var organization = Organization.Create("Agents", $"agents-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var older = Agent.Apply(
            organization.Id,
            "user-1",
            "AG-OLD",
            "REF-OLD",
            DateTimeOffset.UtcNow.AddDays(-1)
        );
        var newer = Agent.Apply(
            organization.Id,
            "user-2",
            "AG-NEW",
            "REF-NEW",
            DateTimeOffset.UtcNow
        );
        newer.SubmitForApproval();
        await AddAsync(older);
        await AddAsync(newer);
        await RunAsAdministratorAsync(organization.Id);

        var page = await SendAsync(new GetAgentApplicationsQuery(organization.Id, 1, 1));
        var searched = await SendAsync(new GetAdminAgentsQuery(organization.Id, Search: "OLD"));

        page.TotalCount.ShouldBe(2);
        page.Items.Single().AgentId.ShouldBe(newer.Id);
        searched.Items.Single().AgentId.ShouldBe(older.Id);
    }

    [Test]
    public async Task AdministratorCannotReadForeignOrganizationQueue()
    {
        var owned = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"foreign-{Guid.NewGuid():N}", "PHP");
        await AddAsync(owned);
        await AddAsync(foreign);
        await AddAsync(
            Agent.Apply(foreign.Id, "foreign-user", "AG-F", "REF-F", DateTimeOffset.UtcNow)
        );
        await RunAsAdministratorAsync(owned.Id);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetAgentApplicationsQuery(foreign.Id))
        );
    }
}
