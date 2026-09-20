using MediatR;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory;
using modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary;
using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary;
using modular_mlm.Application.Compensation.Queries.GetCommissionDetails;
using modular_mlm.Application.Compensation.Queries.GetPairingHistory;
using modular_mlm.Application.Network.Queries.GetAgentLegSummary;
using modular_mlm.Application.Network.Queries.GetBinaryTree;
using modular_mlm.Application.Network.Queries.GetDirectPlacementChildren;
using modular_mlm.Application.Network.Queries.GetDirectRecruits;
using modular_mlm.Application.Network.Queries.GetDownline;
using modular_mlm.Application.Network.Queries.GetPlacementAncestors;
using modular_mlm.Application.Referrals.Commands.RegenerateReferralCode;
using modular_mlm.Application.Referrals.Queries.GetReferralDashboard;
using modular_mlm.Application.Referrals.Queries.GetReferralLink;
using modular_mlm.Application.Wallets.Queries.GetWalletSummary;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Authorization;

using static Infrastructure.TestApp;

public sealed class AgentScopedReadAuthorizationTests : TestBase
{
    [Test]
    public async Task AgentCannotAccessAnotherAgentsPrivateResources()
    {
        var organization = Organization.Create(
            "Private Agent Scope",
            $"private-agent-scope-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var userId = await RunAsUserAsync(
            $"private-agent-{Guid.NewGuid():N}@local",
            "Testing1234!",
            ["Agent"]
        );
        var currentAgent = CreateActiveAgent(organization.Id, userId, "CURRENT");
        var otherAgent = CreateActiveAgent(organization.Id, Guid.NewGuid().ToString(), "OTHER");
        await AddAsync(currentAgent);
        await AddAsync(otherAgent);
        var now = DateTimeOffset.UtcNow;
        IBaseRequest[] protectedRequests =
        [
            new GetWalletSummaryQuery(organization.Id, otherAgent.Id),
            new GetAgentEarningsSummaryQuery(organization.Id, otherAgent.Id),
            new GetAgentCommissionHistoryQuery(organization.Id, otherAgent.Id),
            new GetCommissionDetailsQuery(organization.Id, otherAgent.Id, Guid.NewGuid()),
            new GetBinaryVolumeSummaryQuery(organization.Id, otherAgent.Id),
            new GetPairingHistoryQuery(organization.Id, otherAgent.Id, now.AddDays(-7), now),
            new GetBinaryTreeQuery(organization.Id, otherAgent.Id),
            new GetDownlineQuery(organization.Id, otherAgent.Id),
            new GetDirectPlacementChildrenQuery(organization.Id, otherAgent.Id),
            new GetPlacementAncestorsQuery(organization.Id, otherAgent.Id),
            new GetDirectRecruitsQuery(organization.Id, otherAgent.Id),
            new GetAgentLegSummaryQuery(organization.Id, otherAgent.Id),
            new GetReferralDashboardQuery(organization.Id, otherAgent.Id),
            new GetReferralLinkQuery(organization.Id, otherAgent.Id),
            new RegenerateReferralCodeCommand(organization.Id, otherAgent.Id),
        ];

        foreach (var request in protectedRequests)
            await Should.ThrowAsync<ForbiddenAccessException>(() => SendAsync(request));
    }

    [Test]
    public async Task AgentCanAccessTheirOwnPrivateResources()
    {
        var organization = Organization.Create(
            "Owned Agent Scope",
            $"owned-agent-scope-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var userId = await RunAsUserAsync(
            $"owned-agent-{Guid.NewGuid():N}@local",
            "Testing1234!",
            ["Agent"]
        );
        var agent = CreateActiveAgent(organization.Id, userId, "OWNER");
        await AddAsync(agent);

        var result = await SendAsync(new GetDownlineQuery(organization.Id, agent.Id));

        result.ShouldBeEmpty();
    }

    private static Agent CreateActiveAgent(Guid organizationId, string userId, string prefix)
    {
        var unique = $"{prefix}-{Guid.NewGuid():N}";
        var agent = Agent.Apply(
            organizationId,
            userId,
            unique,
            $"REF-{unique}",
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        return agent;
    }
}
