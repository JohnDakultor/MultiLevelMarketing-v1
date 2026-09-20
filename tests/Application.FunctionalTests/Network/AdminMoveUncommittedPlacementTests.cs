using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Network.Commands.AdminMoveUncommittedPlacement;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Network;

using static Infrastructure.TestApp;

public sealed class AdminMoveUncommittedPlacementTests : TestBase
{
    [Test]
    public async Task AdministratorCanMoveUncommittedLeafAndAuditTheChange()
    {
        var organization = Organization.Create("Move", $"move-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var oldParent = ActiveAgent(organization.Id, "old", "AG-OLD", "REF-OLD");
        var newParent = ActiveAgent(organization.Id, "new", "AG-NEW", "REF-NEW");
        var agent = ActiveAgent(organization.Id, "leaf", "AG-LEAF", "REF-LEAF");
        agent.Place(oldParent.Id, PlacementSide.Left);
        await AddAsync(oldParent);
        await AddAsync(newParent);
        await AddAsync(agent);
        await AddAsync(
            PlacementClosure.Create(organization.Id, oldParent.Id, agent.Id, 1, PlacementSide.Left)
        );
        await RunAsAdministratorAsync(organization.Id);

        await SendAsync(
            new AdminMoveUncommittedPlacementCommand(
                organization.Id,
                agent.Id,
                newParent.Id,
                PlacementSide.Right,
                oldParent.Id,
                PlacementSide.Left,
                "Correct enrollment placement"
            )
        );

        var moved = await FindAsync<Agent>(agent.Id);
        moved!.PlacementParentAgentId.ShouldBe(newParent.Id);
        moved.PlacementSide.ShouldBe(PlacementSide.Right);
        (
            await CountAsync<PlacementClosure>(row =>
                row.OrganizationId == organization.Id
                && row.AncestorAgentId == newParent.Id
                && row.DescendantAgentId == agent.Id
            )
        ).ShouldBe(1);
        (
            await CountAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == agent.Id
                && entry.Reason == "Correct enrollment placement"
            )
        ).ShouldBe(1);
    }

    [Test]
    public async Task StaleExpectedPlacementIsAConflictAndDoesNotMoveAgent()
    {
        var organization = Organization.Create("Move", $"move-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var oldParent = ActiveAgent(organization.Id, "old", "AG-OLD", "REF-OLD");
        var newParent = ActiveAgent(organization.Id, "new", "AG-NEW", "REF-NEW");
        var agent = ActiveAgent(organization.Id, "leaf", "AG-LEAF", "REF-LEAF");
        agent.Place(oldParent.Id, PlacementSide.Left);
        await AddAsync(oldParent);
        await AddAsync(newParent);
        await AddAsync(agent);
        await AddAsync(
            PlacementClosure.Create(organization.Id, oldParent.Id, agent.Id, 1, PlacementSide.Left)
        );
        await RunAsAdministratorAsync(organization.Id);

        await Should.ThrowAsync<PlacementConflictException>(() =>
            SendAsync(
                new AdminMoveUncommittedPlacementCommand(
                    organization.Id,
                    agent.Id,
                    newParent.Id,
                    PlacementSide.Right,
                    Guid.NewGuid(),
                    PlacementSide.Left,
                    "Stale request"
                )
            )
        );

        var unchanged = await FindAsync<Agent>(agent.Id);
        unchanged!.PlacementParentAgentId.ShouldBe(oldParent.Id);
    }

    private static Agent ActiveAgent(
        Guid organizationId,
        string userId,
        string agentCode,
        string referralCode
    )
    {
        var agent = Agent.Apply(
            organizationId,
            userId,
            agentCode,
            referralCode,
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        return agent;
    }
}
