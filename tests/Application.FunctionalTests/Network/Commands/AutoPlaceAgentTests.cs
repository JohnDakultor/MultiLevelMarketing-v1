using modular_mlm.Application.Network.Commands.AutoPlaceAgent;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Network.Commands;

using static Infrastructure.TestApp;

public sealed class AutoPlaceAgentTests : TestBase
{
    [Test]
    public async Task ShouldPlaceAgentAndCreateClosureRow()
    {
        var organization = Organization.Create("Placement Test", "placement-test", "USD");
        var sponsor = CreateActiveAgent(organization.Id, "SPONSOR");
        var agent = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            "AGENT-1",
            "REF-1",
            DateTimeOffset.UtcNow,
            sponsor.Id
        );

        await AddAsync(organization);
        await AddAsync(sponsor);
        await AddAsync(agent);
        await RunAsAdministratorAsync(organization.Id);

        var decision = await SendAsync(new AutoPlaceAgentCommand(organization.Id, agent.Id));

        decision.ParentAgentId.ShouldBe(sponsor.Id);
        decision.Side.ShouldBe(PlacementSide.Left);

        var storedAgent = await FindAsync<Agent>(agent.Id);
        storedAgent.ShouldNotBeNull();
        storedAgent.PlacementParentAgentId.ShouldBe(sponsor.Id);
        storedAgent.PlacementSide.ShouldBe(PlacementSide.Left);
        (await CountAsync<PlacementClosure>()).ShouldBe(1);
    }

    [Test]
    public async Task ShouldAllocateDifferentSlotsWhenRequestsAreConcurrent()
    {
        var organization = Organization.Create("Concurrency Test", "concurrency-test", "USD");
        var sponsor = CreateActiveAgent(organization.Id, "SPONSOR");
        var firstAgent = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            "AGENT-1",
            "REF-1",
            DateTimeOffset.UtcNow,
            sponsor.Id
        );
        var secondAgent = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            "AGENT-2",
            "REF-2",
            DateTimeOffset.UtcNow,
            sponsor.Id
        );

        await AddAsync(organization);
        await AddAsync(sponsor);
        await AddAsync(firstAgent);
        await AddAsync(secondAgent);
        await RunAsAdministratorAsync(organization.Id);

        var decisions = await Task.WhenAll(
            SendAsync(new AutoPlaceAgentCommand(organization.Id, firstAgent.Id)),
            SendAsync(new AutoPlaceAgentCommand(organization.Id, secondAgent.Id))
        );

        decisions.Select(x => x.ParentAgentId).ShouldAllBe(x => x == sponsor.Id);
        decisions.Select(x => x.Side).ShouldBe([PlacementSide.Left, PlacementSide.Right], true);
        (await CountAsync<PlacementClosure>()).ShouldBe(2);
    }

    private static Agent CreateActiveAgent(Guid organizationId, string code)
    {
        var agent = Agent.Apply(
            organizationId,
            Guid.NewGuid().ToString(),
            code,
            $"REF-{code}",
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        return agent;
    }
}
