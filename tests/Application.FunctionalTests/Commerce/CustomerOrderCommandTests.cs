using Domain.Enums;
using modular_mlm.Application.Commerce.Commands.RequestCancellation;
using modular_mlm.Application.Commerce.Commands.RequestRefund;
using modular_mlm.Application.Commerce.Queries.GetMyOrders;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class CustomerOrderCommandTests : TestBase
{
    [Test]
    public async Task CustomerCanCancelTheirUnpaidUnfulfilledOrder()
    {
        var data = await CreateOrderAsync(delivered: false);

        await SendAsync(
            new RequestCancellationCommand(
                data.Organization.Id,
                data.Order.Id,
                "Ordered by mistake"
            )
        );

        var order = await SingleAsync<Order>(candidate => candidate.Id == data.Order.Id);
        var item = await SingleAsync<OrderItem>(candidate => candidate.Id == data.Item.Id);
        order.Status.ShouldBe(OrderStatus.Cancelled);
        item.FulfillmentStatus.ShouldBe(FulfillmentStatus.Cancelled);
        (
            await CountAsync<AuditLog>(entry =>
                entry.OrganizationId == data.Organization.Id
                && entry.EntityId == data.Order.Id
                && entry.Action == AuditCoverageMap.OrderCancelled.Action
            )
        ).ShouldBe(1);
    }

    [Test]
    public async Task DeliveredOrderWithinReturnWindowCanRequestItemRefund()
    {
        var data = await CreateOrderAsync(delivered: true);

        var refundId = await SendAsync(
            new RequestRefundCommand(
                data.Organization.Id,
                data.Order.Id,
                data.Item.Id,
                1,
                "Item was damaged"
            )
        );

        var refund = await FindAsync<OrderItemRefund>(refundId);
        refund.ShouldNotBeNull();
        refund.OrderId.ShouldBe(data.Order.Id);
        refund.OrderItemId.ShouldBe(data.Item.Id);
        refund.Quantity.ShouldBe(1m);
        GetRequiredService<TestPaymentGateway>().RefundCallCount.ShouldBe(1);
    }

    [Test]
    public async Task MyOrdersReturnsOnlyCurrentCustomersOrders()
    {
        var data = await CreateOrderAsync(delivered: false);
        var foreignOrder = Order.Create(
            data.Organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            null,
            "PHP",
            "{}",
            "{}"
        );
        foreignOrder.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Foreign product",
            "FOREIGN-SKU",
            10m,
            1,
            10m,
            null,
            1m,
            null
        );
        await AddAsync(foreignOrder);

        var result = await SendAsync(new GetMyOrdersQuery(data.Organization.Id, 1, 20, null));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Id.ShouldBe(data.Order.Id);
    }

    private static async Task<CustomerOrderData> CreateOrderAsync(bool delivered)
    {
        var organization = Organization.Create(
            "Customer Orders",
            $"customer-orders-{Guid.NewGuid():N}",
            "PHP"
        );
        organization.UpdateWalletSettings(
            WalletSettings.Create(CommissionReleaseTrigger.PaymentConfirmed, 0, 30, 10m, false, 0m)
        );
        var userId = await RunAsUserAsync(
            $"order-customer-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        var customer = CustomerProfile.Create(organization.Id, userId, "Order Customer");
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            customer.Id,
            null,
            "PHP",
            "{}",
            "{}"
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Customer product",
            "CUSTOMER-SKU",
            100m,
            2,
            150m,
            null,
            20m,
            null
        );
        var item = order.Items.Single();
        Payment? payment = null;
        if (delivered)
        {
            var now = DateTimeOffset.UtcNow;
            order.MarkPaid(now);
            order.StartProcessing();
            order.Ship();
            order.Deliver(now);
            payment = Payment.Initiate(
                organization.Id,
                order.Id,
                "PayMongo",
                $"payment:{order.Id:N}",
                order.GrandTotal,
                order.Currency
            );
            payment.MarkPaid($"pay_{Guid.NewGuid():N}", now);
        }

        await AddAsync(organization);
        await AddAsync(customer);
        await AddAsync(order);
        if (payment is not null)
            await AddAsync(payment);

        return new CustomerOrderData(organization, order, item);
    }

    private sealed record CustomerOrderData(Organization Organization, Order Order, OrderItem Item);
}
