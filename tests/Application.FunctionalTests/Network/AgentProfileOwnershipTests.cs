using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Network.Queries.GetAgentProfile;
using modular_mlm.Application.Network.Queries.GetCurrentAgentContext;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Network;

using static Infrastructure.TestApp;

public sealed class AgentProfileOwnershipTests : TestBase
{
    [Test]
    public async Task CurrentAgentCanReadOwnContextAndProfileButNotPeerProfile()
    {
        var organization = Organization.Create("Network", $"network-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var userId = await RunAsUserAsync(
            $"agent-{Guid.NewGuid():N}@local",
            "Agent1234!",
            [Roles.Agent]
        );
        var current = Agent.Apply(
            organization.Id,
            userId,
            "AG-ME",
            "REF-ME",
            DateTimeOffset.UtcNow
        );
        current.Activate(DateTimeOffset.UtcNow);
        var peer = Agent.Apply(
            organization.Id,
            "peer-user",
            "AG-PEER",
            "REF-PEER",
            DateTimeOffset.UtcNow
        );
        peer.Activate(DateTimeOffset.UtcNow);
        await AddAsync(current);
        await AddAsync(peer);

        var context = await SendAsync(new GetCurrentAgentContextQuery(organization.Id));
        var profile = await SendAsync(new GetAgentProfileQuery(organization.Id, current.Id));

        context!.AgentId.ShouldBe(current.Id);
        profile!.AgentId.ShouldBe(current.Id);
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetAgentProfileQuery(organization.Id, peer.Id))
        );
    }

    [Test]
    public async Task OrganizationAdministratorCanReadOnlyInTenantAgent()
    {
        var owned = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"foreign-{Guid.NewGuid():N}", "PHP");
        await AddAsync(owned);
        await AddAsync(foreign);
        var ownedAgent = Agent.Apply(
            owned.Id,
            "owned-user",
            "AG-O",
            "REF-O",
            DateTimeOffset.UtcNow
        );
        var foreignAgent = Agent.Apply(
            foreign.Id,
            "foreign-user",
            "AG-F",
            "REF-F",
            DateTimeOffset.UtcNow
        );
        await AddAsync(ownedAgent);
        await AddAsync(foreignAgent);
        await RunAsAdministratorAsync(owned.Id);

        (await SendAsync(new GetAgentProfileQuery(owned.Id, ownedAgent.Id))).ShouldNotBeNull();
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetAgentProfileQuery(foreign.Id, foreignAgent.Id))
        );
    }
}
