using modular_mlm.Application.Commerce.Commands.CreatePaymentSession;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Network.Commands.ApplyAsAgent;
using modular_mlm.Application.Network.Queries.GetAgentApplication;
using modular_mlm.Application.Wallets.Queries.GetWalletSummary;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Authorization;

using static Infrastructure.TestApp;

public sealed class RequestScopeAuthorizationTests : TestBase
{
    [Test]
    public async Task AgentCannotReadAnotherAgentsWallet()
    {
        var organization = Organization.Create(
            "Agent Scope",
            $"agent-scope-{Guid.NewGuid():N}",
            "PHP"
        );
        var currentUserId = await RunAsUserAsync(
            $"agent-a-{Guid.NewGuid():N}@local",
            "Testing1234!",
            ["Agent"]
        );
        var currentAgent = Agent.Apply(
            organization.Id,
            currentUserId,
            $"AG-{Guid.NewGuid():N}",
            $"REF-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow
        );
        var otherAgent = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            $"AG-{Guid.NewGuid():N}",
            $"REF-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow
        );
        await AddAsync(organization);
        await AddAsync(currentAgent);
        await AddAsync(otherAgent);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetWalletSummaryQuery(organization.Id, otherAgent.Id))
        );
    }

    [Test]
    public async Task CustomerCannotCreatePaymentSessionForAnotherCustomersOrder()
    {
        var organization = Organization.Create(
            "Customer Scope",
            $"customer-scope-{Guid.NewGuid():N}",
            "PHP"
        );
        var ownerUserId = await RunAsUserAsync(
            $"owner-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        var owner = CustomerProfile.Create(organization.Id, ownerUserId, "Owner");
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            owner.Id,
            null,
            "PHP",
            "{}",
            "{}"
        );
        await RunAsUserAsync($"other-customer-{Guid.NewGuid():N}@local", "Testing1234!", []);
        await AddAsync(organization);
        await AddAsync(owner);
        await AddAsync(order);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(
                new CreatePaymentSessionCommand(
                    organization.Id,
                    order.Id,
                    new Uri("https://frontend.test/payment/success"),
                    new Uri("https://frontend.test/payment/cancel"),
                    ["gcash"]
                )
            )
        );
    }

    [Test]
    public async Task AgentApplicationUsesTheAuthenticatedIdentity()
    {
        var organization = Organization.Create(
            "Application Scope",
            $"application-scope-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var userId = await RunAsUserAsync(
            $"applicant-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );

        var agentId = await SendAsync(new ApplyAsAgentCommand(organization.Id, null));

        var agent = await FindAsync<Agent>(agentId);
        agent.ShouldNotBeNull();
        agent.UserId.ShouldBe(userId);
    }

    [Test]
    public async Task AgentApplicationRequiresAnAuthenticatedIdentity()
    {
        var organization = Organization.Create(
            "Anonymous Application",
            $"anonymous-application-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            SendAsync(new ApplyAsAgentCommand(organization.Id, null))
        );
    }

    [Test]
    public async Task AuthenticatedNonAgentCanCheckApplicationStatus()
    {
        var organization = Organization.Create(
            "Application Status",
            $"application-status-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsUserAsync(
            $"new-applicant-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );

        var application = await SendAsync(new GetAgentApplicationQuery(organization.Id));

        application.ShouldBeNull();
    }
}
