using modular_mlm.Application.Commerce.Commands.AdminCancelOrder;
using modular_mlm.Application.Commerce.Commands.MarkOrderDelivered;
using modular_mlm.Application.Commerce.Commands.MarkOrderShipped;
using modular_mlm.Application.Commerce.Commands.StartOrderProcessing;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class OrderFulfillmentCommandTests : TestBase
{
    [Test]
    public async Task AdministratorCanProcessShipAndDeliverOrder()
    {
        var (organization, order) = await CreateOrderAsync(paid: true);
        await RunAsAdministratorAsync(organization.Id);

        await SendAsync(new StartOrderProcessingCommand(organization.Id, order.Id));
        await SendAsync(new MarkOrderShippedCommand(
            organization.Id, order.Id, " DHL ", " TRACK-123 "
        ));
        await SendAsync(new MarkOrderDeliveredCommand(organization.Id, order.Id));

        var saved = await SingleAsync<Order>(candidate => candidate.Id == order.Id);
        saved.Status.ShouldBe(OrderStatus.Delivered);
        saved.ShippingCarrier.ShouldBe("DHL");
        saved.TrackingNumber.ShouldBe("TRACK-123");
        saved.ShippedAt.ShouldNotBeNull();
        saved.DeliveredAt.ShouldNotBeNull();
        (await CountAsync<AuditLog>(entry =>
            entry.OrganizationId == organization.Id
            && entry.EntityId == order.Id
            && (entry.Action == AuditCoverageMap.OrderProcessingStarted.Action
                || entry.Action == AuditCoverageMap.OrderShipped.Action
                || entry.Action == AuditCoverageMap.OrderDelivered.Action)
        )).ShouldBe(3);
    }

    [Test]
    public async Task AdministratorCanCancelUnpaidUnfulfilledOrder()
    {
        var (organization, order) = await CreateOrderAsync(paid: false);
        await RunAsAdministratorAsync(organization.Id);

        await SendAsync(new AdminCancelOrderCommand(organization.Id, order.Id, " Customer request "));

        var saved = await SingleAsync<Order>(candidate => candidate.Id == order.Id);
        saved.Status.ShouldBe(OrderStatus.Cancelled);
        (await CountAsync<AuditLog>(entry =>
            entry.OrganizationId == organization.Id
            && entry.EntityId == order.Id
            && entry.Action == AuditCoverageMap.OrderCancelled.Action
        )).ShouldBe(1);
    }

    [Test]
    public async Task FulfillmentCannotAccessAnotherTenantOrder()
    {
        var (ownedOrganization, _) = await CreateOrderAsync(paid: true);
        var (_, foreignOrder) = await CreateOrderAsync(paid: true);
        await RunAsAdministratorAsync(ownedOrganization.Id);

        await Should.ThrowAsync<KeyNotFoundException>(() => SendAsync(
            new StartOrderProcessingCommand(ownedOrganization.Id, foreignOrder.Id)
        ));
    }

    [Test]
    public async Task ConcurrentFulfillmentWritesAreRejectedByDatabaseToken()
    {
        var (_, order) = await CreateOrderAsync(paid: true);
        await using var firstScope = FunctionalTestSetup.ScopeFactory.CreateAsyncScope();
        await using var secondScope = FunctionalTestSetup.ScopeFactory.CreateAsyncScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var first = await firstDb.Orders.Include(candidate => candidate.Items)
            .SingleAsync(candidate => candidate.Id == order.Id);
        var second = await secondDb.Orders.Include(candidate => candidate.Items)
            .SingleAsync(candidate => candidate.Id == order.Id);

        first.StartProcessing();
        second.StartProcessing();
        await firstDb.SaveChangesAsync();

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => secondDb.SaveChangesAsync());
    }

    private static async Task<(Organization Organization, Order Order)> CreateOrderAsync(bool paid)
    {
        var organization = Organization.Create(
            "Fulfillment Tests",
            $"fulfillment-{Guid.NewGuid():N}",
            "USD"
        );
        var customer = CustomerProfile.Create(organization.Id, Guid.NewGuid().ToString(), "Customer");
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            customer.Id,
            null,
            "USD",
            "{}",
            "{}"
        );
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), "Product", "SKU", 100m, 1, 100m, null, 10m, null
        );
        if (paid)
            order.MarkPaid(DateTimeOffset.UtcNow);

        await AddAsync(organization);
        await AddAsync(customer);
        await AddAsync(order);
        return (organization, order);
    }
}
