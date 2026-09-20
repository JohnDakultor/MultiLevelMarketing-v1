using modular_mlm.Domain.Commerce;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Commerce;

public sealed class OrderTests
{
    [Test]
    public void PaidOrderCannotAcceptMoreItems()
    {
        var order = Order.Create(Guid.NewGuid(), "ORD-1", Guid.NewGuid(), null, "USD", "{}", "{}");
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "SKU",
            100m,
            1,
            100m,
            null,
            10m,
            null
        );
        order.MarkPaid(DateTimeOffset.UtcNow);
        Should.Throw<Exception>(() =>
            order.AddItem(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Other",
                "SKU2",
                20m,
                1,
                20m,
                null,
                2m,
                null
            )
        );
    }

    [Test]
    public void OrderFulfillmentTransitionsKeepItemStateInSync()
    {
        var order = Order.Create(Guid.NewGuid(), "ORD-2", Guid.NewGuid(), null, "USD", "{}", "{}");
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "SKU",
            100m,
            1,
            100m,
            null,
            10m,
            null
        );
        var item = order.Items.Single();

        order.MarkPaid(DateTimeOffset.UtcNow);
        order.StartProcessing();
        item.FulfillmentStatus.ShouldBe(FulfillmentStatus.Processing);
        order.Ship();
        item.FulfillmentStatus.ShouldBe(FulfillmentStatus.Shipped);
        order.Deliver(DateTimeOffset.UtcNow);
        item.FulfillmentStatus.ShouldBe(FulfillmentStatus.Delivered);
    }

    [Test]
    public void CancellingPendingOrderCancelsItsItems()
    {
        var order = Order.Create(Guid.NewGuid(), "ORD-3", Guid.NewGuid(), null, "USD", "{}", "{}");
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "SKU",
            100m,
            1,
            100m,
            null,
            10m,
            null
        );

        order.Cancel();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.Items.Single().FulfillmentStatus.ShouldBe(FulfillmentStatus.Cancelled);
    }
}
