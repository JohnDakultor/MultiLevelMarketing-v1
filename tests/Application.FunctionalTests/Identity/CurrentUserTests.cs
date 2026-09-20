using modular_mlm.Application.Identity.Queries.GetCurrentUser;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Identity;

public sealed class CurrentUserTests : TestBase
{
    [Test]
    public async Task ReturnsTrustedOrganizationCustomerAndAgentContext()
    {
        var organization = Organization.Create(
            "Identity Org",
            $"identity-{Guid.NewGuid():N}",
            "PHP"
        );
        await TestApp.AddAsync(organization);
        var userId = await TestApp.RunAsAdministratorAsync(organization.Id);
        var customer = CustomerProfile.Create(organization.Id, userId, "Current User");
        var agent = Agent.Apply(
            organization.Id,
            userId,
            $"A{Guid.NewGuid():N}"[..12],
            $"R{Guid.NewGuid():N}"[..12],
            DateTimeOffset.UtcNow
        );
        await TestApp.AddAsync(customer);
        await TestApp.AddAsync(agent);

        var result = await TestApp.SendAsync(new GetCurrentUserQuery());

        result.UserId.ShouldBe(userId);
        result.OrganizationId.ShouldBe(organization.Id);
        result.CustomerId.ShouldBe(customer.Id);
        result.AgentId.ShouldBe(agent.Id);
        result.Roles.ShouldContain(Roles.Administrator);
    }
}
