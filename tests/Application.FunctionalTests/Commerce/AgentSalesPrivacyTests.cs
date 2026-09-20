using modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails;
using modular_mlm.Application.Commerce.Queries.GetAttributedOrders;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class AgentSalesPrivacyTests : TestBase
{
    [Test]
    public async Task AgentSeesOnlyOwnAttributedOrdersWithMaskedCustomerData()
    {
        var organization = Organization.Create("Sales", $"sales-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var userId = await RunAsUserAsync(
            $"agent-{Guid.NewGuid():N}@local",
            "Agent1234!",
            [Roles.Agent]
        );
        var agent = Agent.Apply(organization.Id, userId, "AG-1", "REF-1", DateTimeOffset.UtcNow);
        agent.Activate(DateTimeOffset.UtcNow);
        await AddAsync(agent);
        var customer = CustomerProfile.Create(organization.Id, "customer-user", "Maria Santos");
        await AddAsync(customer);
        var order = CreatePaidOrder(organization.Id, customer.Id, agent.Id);
        await AddAsync(order);

        var page = await SendAsync(new GetAttributedOrdersQuery(organization.Id));
        var details = await SendAsync(
            new GetAttributedOrderDetailsQuery(organization.Id, order.Id)
        );

        page.TotalCount.ShouldBe(1);
        page.Items.Single().MaskedCustomerName.ShouldNotBe("Maria Santos");
        details.ShouldNotBeNull();
        details.MaskedCustomerName.ShouldNotBe("Maria Santos");
        details.Items.Single().Sku.ShouldBe("SKU-1");
    }

    [Test]
    public async Task AgentCannotUseAnotherAgentsOrderIdentifier()
    {
        var organization = Organization.Create("Sales", $"sales-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var owner = Agent.Apply(
            organization.Id,
            "owner",
            "AG-OWNER",
            "REF-OWNER",
            DateTimeOffset.UtcNow
        );
        owner.Activate(DateTimeOffset.UtcNow);
        await AddAsync(owner);
        var customer = CustomerProfile.Create(organization.Id, "customer", "Private Customer");
        await AddAsync(customer);
        var order = CreatePaidOrder(organization.Id, customer.Id, owner.Id);
        await AddAsync(order);
        var intruderUserId = await RunAsUserAsync(
            $"agent-{Guid.NewGuid():N}@local",
            "Agent1234!",
            [Roles.Agent]
        );
        var intruder = Agent.Apply(
            organization.Id,
            intruderUserId,
            "AG-INTRUDER",
            "REF-INTRUDER",
            DateTimeOffset.UtcNow
        );
        intruder.Activate(DateTimeOffset.UtcNow);
        await AddAsync(intruder);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetAttributedOrderDetailsQuery(organization.Id, order.Id))
        );
    }

    private static Order CreatePaidOrder(Guid organizationId, Guid customerId, Guid agentId)
    {
        var order = Order.Create(
            organizationId,
            $"ORD-{Guid.NewGuid():N}",
            customerId,
            agentId,
            "PHP",
            "{\"redacted\":true}",
            "{\"redacted\":true}"
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "SKU-1",
            100m,
            1,
            80m,
            null,
            25m,
            null
        );
        order.MarkPaid(DateTimeOffset.UtcNow);
        return order;
    }
}
