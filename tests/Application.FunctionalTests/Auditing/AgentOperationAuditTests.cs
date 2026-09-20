using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Network.Commands.ActivateAgent;
using modular_mlm.Application.Network.Commands.ApproveAgent;
using modular_mlm.Application.Network.Commands.AutoPlaceAgent;
using modular_mlm.Application.Network.Commands.PlaceAgent;
using modular_mlm.Application.Network.Commands.ReactivateAgent;
using modular_mlm.Application.Network.Commands.RejectAgent;
using modular_mlm.Application.Network.Commands.SuspendAgent;
using modular_mlm.Application.Referrals.Commands.RegenerateReferralCode;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Application.FunctionalTests.Auditing;

using static Infrastructure.TestApp;

public sealed class AgentOperationAuditTests : TestBase
{
    [Test]
    public async Task ApprovalCreatesAgentAuditWithTrustedActorAndStateTransition()
    {
        var organization = await CreateOrganizationAsync();
        var agentUserId = await RunAsUserAsync(
            $"agent-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        var agent = CreateAgent(organization.Id, userId: agentUserId);
        agent.SubmitForApproval();
        await AddAsync(agent);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await SendAsync(new ApproveAgentCommand(organization.Id, agent.Id));

        await AssertAuditAsync(
            organization.Id,
            agent.Id,
            actorId,
            AuditCoverageMap.AgentApproved,
            AgentStatus.PendingApproval,
            AgentStatus.Active
        );
        await AssertAgentIdentityAccessAsync(agentUserId, organization.Id);
    }

    [Test]
    public async Task RejectionCreatesAgentAudit()
    {
        var organization = await CreateOrganizationAsync();
        var agent = CreateAgent(organization.Id);
        await AddAsync(agent);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await SendAsync(new RejectAgentCommand(organization.Id, agent.Id));
        await SendAsync(new RejectAgentCommand(organization.Id, agent.Id));

        await AssertAuditAsync(
            organization.Id,
            agent.Id,
            actorId,
            AuditCoverageMap.AgentRejected,
            AgentStatus.Applied,
            AgentStatus.Closed
        );
        (await CountAsync<AuditLog>()).ShouldBe(1);
    }

    [Test]
    public async Task ActivationCreatesAgentAudit()
    {
        var organization = await CreateOrganizationAsync();
        var agentUserId = await RunAsUserAsync(
            $"agent-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        var agent = CreateAgent(organization.Id, userId: agentUserId);
        await AddAsync(agent);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await SendAsync(new ActivateAgentCommand(organization.Id, agent.Id));

        await AssertAuditAsync(
            organization.Id,
            agent.Id,
            actorId,
            AuditCoverageMap.AgentActivated,
            AgentStatus.Applied,
            AgentStatus.Active
        );
        await AssertAgentIdentityAccessAsync(agentUserId, organization.Id);
    }

    [Test]
    public async Task SuspensionAndReactivationEachCreateAnAudit()
    {
        var organization = await CreateOrganizationAsync();
        var agent = CreateActiveAgent(organization.Id);
        await AddAsync(agent);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await SendAsync(new SuspendAgentCommand(organization.Id, agent.Id));
        await SendAsync(new ReactivateAgentCommand(organization.Id, agent.Id));

        await AssertAuditAsync(
            organization.Id,
            agent.Id,
            actorId,
            AuditCoverageMap.AgentSuspended,
            AgentStatus.Active,
            AgentStatus.Suspended
        );
        await AssertAuditAsync(
            organization.Id,
            agent.Id,
            actorId,
            AuditCoverageMap.AgentReactivated,
            AgentStatus.Suspended,
            AgentStatus.Active
        );
    }

    [Test]
    public async Task ManualPlacementCommitsPlacementClosureAndAuditTogether()
    {
        var organization = await CreateOrganizationAsync();
        var parent = CreateActiveAgent(organization.Id);
        var agent = CreateAgent(organization.Id);
        await AddAsync(parent);
        await AddAsync(agent);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await SendAsync(
            new PlaceAgentCommand(organization.Id, agent.Id, parent.Id, PlacementSide.Left)
        );

        var audit = await AssertAuditAsync(
            organization.Id,
            agent.Id,
            actorId,
            AuditCoverageMap.AgentPlaced,
            AgentStatus.Applied,
            AgentStatus.Applied
        );
        ReadProperty(audit.BeforeJson, "PlacementParentAgentId")
            .ValueKind.ShouldBe(JsonValueKind.Null);
        ReadProperty(audit.AfterJson, "PlacementParentAgentId").GetGuid().ShouldBe(parent.Id);
        (
            await CountAsync<PlacementClosure>(closure => closure.DescendantAgentId == agent.Id)
        ).ShouldBe(1);
    }

    [Test]
    public async Task AutomaticPlacementCommitsAuditInsidePlacementTransaction()
    {
        var organization = await CreateOrganizationAsync();
        var sponsor = CreateActiveAgent(organization.Id);
        var agent = CreateAgent(organization.Id, sponsor.Id);
        await AddAsync(sponsor);
        await AddAsync(agent);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        var decision = await SendAsync(new AutoPlaceAgentCommand(organization.Id, agent.Id));

        var audit = await AssertAuditAsync(
            organization.Id,
            agent.Id,
            actorId,
            AuditCoverageMap.AgentAutoPlaced,
            AgentStatus.Applied,
            AgentStatus.Applied
        );
        ReadProperty(audit.AfterJson, "PlacementParentAgentId")
            .GetGuid()
            .ShouldBe(decision.ParentAgentId);
        (
            await CountAsync<PlacementClosure>(closure => closure.DescendantAgentId == agent.Id)
        ).ShouldBe(1);
    }

    [Test]
    public async Task ReferralCodeRegenerationAuditsOldAndNewCode()
    {
        var organization = await CreateOrganizationAsync();
        var agent = CreateActiveAgent(organization.Id);
        var oldCode = agent.ReferralCode;
        await AddAsync(agent);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        var newCode = await SendAsync(new RegenerateReferralCodeCommand(organization.Id, agent.Id));

        var audit = await AssertAuditAsync(
            organization.Id,
            agent.Id,
            actorId,
            AuditCoverageMap.ReferralCodeRegenerated,
            AgentStatus.Active,
            AgentStatus.Active
        );
        ReadProperty(audit.BeforeJson, "ReferralCode").GetString().ShouldBe(oldCode);
        ReadProperty(audit.AfterJson, "ReferralCode").GetString().ShouldBe(newCode);
    }

    [Test]
    public async Task FailedMutationAndCrossOrganizationRequestCreateNoAudit()
    {
        var owned = await CreateOrganizationAsync("owned");
        var foreign = await CreateOrganizationAsync("foreign");
        var invalidStateAgent = CreateAgent(owned.Id);
        var foreignAgent = CreateAgent(foreign.Id);
        await AddAsync(invalidStateAgent);
        await AddAsync(foreignAgent);
        await RunAsAdministratorAsync(owned.Id);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(new ApproveAgentCommand(owned.Id, invalidStateAgent.Id))
        );
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new RejectAgentCommand(foreign.Id, foreignAgent.Id))
        );

        (await CountAsync<AuditLog>()).ShouldBe(0);
    }

    private static async Task<AuditLog> AssertAuditAsync(
        Guid organizationId,
        Guid agentId,
        Guid actorId,
        AuditCoverageDefinition definition,
        AgentStatus beforeStatus,
        AgentStatus afterStatus
    )
    {
        var audit = await SingleAsync<AuditLog>(candidate =>
            candidate.OrganizationId == organizationId
            && candidate.EntityId == agentId
            && candidate.Action == definition.Action
        );

        audit.EntityType.ShouldBe(definition.EntityType);
        audit.ActorUserId.ShouldBe(actorId);
        ReadProperty(audit.BeforeJson, "Status").GetInt32().ShouldBe((int)beforeStatus);
        ReadProperty(audit.AfterJson, "Status").GetInt32().ShouldBe((int)afterStatus);
        return audit;
    }

    private static JsonElement ReadProperty(string? json, string propertyName)
    {
        json.ShouldNotBeNullOrWhiteSpace();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(propertyName).Clone();
    }

    private static async Task<Organization> CreateOrganizationAsync(string name = "agent-audit")
    {
        var organization = Organization.Create(
            "Agent Audit Test",
            $"{name}-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        return organization;
    }

    private static Agent CreateAgent(
        Guid organizationId,
        Guid? sponsorAgentId = null,
        string? userId = null
    ) =>
        Agent.Apply(
            organizationId,
            userId ?? Guid.NewGuid().ToString(),
            $"AG{Guid.NewGuid():N}"[..12],
            $"RF{Guid.NewGuid():N}"[..12],
            DateTimeOffset.UtcNow,
            sponsorAgentId
        );

    private static async Task AssertAgentIdentityAccessAsync(string userId, Guid organizationId)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var identity = await users.FindByIdAsync(userId);

        identity.ShouldNotBeNull();
        identity.OrganizationId.ShouldBe(organizationId);
        (await users.IsInRoleAsync(identity, Roles.Agent)).ShouldBeTrue();
    }

    private static Agent CreateActiveAgent(Guid organizationId)
    {
        var agent = CreateAgent(organizationId);
        agent.Activate(DateTimeOffset.UtcNow);
        return agent;
    }
}
