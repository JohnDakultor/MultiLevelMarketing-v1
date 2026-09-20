using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Network.Commands.PlaceAgent;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Network;

using static Infrastructure.TestApp;

public sealed class ConcurrentAgentPlacementTests : TestBase
{
    [Test]
    public async Task OnlyOneConcurrentPlacementCanClaimAParentSide()
    {
        var organization = Organization.Create("Race", $"race-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var parent = Agent.Apply(organization.Id, "parent", "AG-P", "REF-P", DateTimeOffset.UtcNow);
        parent.Activate(DateTimeOffset.UtcNow);
        var first = Agent.Apply(organization.Id, "first", "AG-1", "REF-1", DateTimeOffset.UtcNow);
        var second = Agent.Apply(organization.Id, "second", "AG-2", "REF-2", DateTimeOffset.UtcNow);
        await AddAsync(parent);
        await AddAsync(first);
        await AddAsync(second);
        await RunAsAdministratorAsync(organization.Id);

        var attempts = await Task.WhenAll(TryPlace(first.Id), TryPlace(second.Id));

        attempts.Count(result => result).ShouldBe(1);
        (
            await CountAsync<Agent>(agent =>
                agent.OrganizationId == organization.Id
                && agent.PlacementParentAgentId == parent.Id
                && agent.PlacementSide == PlacementSide.Left
            )
        ).ShouldBe(1);
        (
            await CountAsync<PlacementClosure>(closure =>
                closure.OrganizationId == organization.Id
                && closure.AncestorAgentId == parent.Id
                && closure.Depth == 1
            )
        ).ShouldBe(1);

        async Task<bool> TryPlace(Guid agentId)
        {
            try
            {
                await SendAsync(
                    new PlaceAgentCommand(organization.Id, agentId, parent.Id, PlacementSide.Left)
                );
                return true;
            }
            catch (PlacementConflictException)
            {
                return false;
            }
        }
    }
}
