using modular_mlm.Application.Commerce.Commands.HandlePaymentWebhook;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class HandlePaymentWebhookTests : TestBase
{
    [Test]
    public async Task ShouldMarkTheOrderPaidAndDeduplicateTheProviderEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "PayMongo Test",
            $"paymongo-test-{Guid.NewGuid():N}",
            "PHP"
        );
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            null,
            "PHP",
            "{}",
            "{}"
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test product",
            "TEST-SKU",
            100m,
            1,
            100m,
            null,
            0m,
            null
        );
        var payment = Payment.Initiate(
            organization.Id,
            order.Id,
            "PayMongo",
            $"paymongo:{order.Id:N}",
            order.GrandTotal,
            order.Currency
        );
        payment.AttachCheckoutSession(
            "cs_functional_test",
            new Uri("https://checkout.paymongo.com/test")
        );
        await AddAsync(organization);
        await AddAsync(order);
        await AddAsync(payment);
        var command = new HandlePaymentWebhookCommand(
            "evt_functional_test",
            "checkout_session.payment.paid",
            "cs_functional_test",
            order.OrderNumber,
            "pay_functional_test",
            now,
            new string('a', 64)
        );

        (await SendAsync(command)).ShouldBeTrue();
        (await SendAsync(command)).ShouldBeFalse();

        var updatedOrder = await SingleAsync<Order>(candidate => candidate.Id == order.Id);
        var updatedPayment = await SingleAsync<Payment>(candidate => candidate.Id == payment.Id);
        updatedOrder.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        updatedPayment.Status.ShouldBe(PaymentStatus.Paid);
        updatedPayment.ProviderPaymentId.ShouldBe("pay_functional_test");
        (await CountAsync<PaymentWebhookReceipt>()).ShouldBe(1);
    }
}
