using modular_mlm.Application.Commerce.Commands.ReconcilePayment;
using modular_mlm.Application.Commerce.Commands.RequestPaymentRefund;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.FunctionalTests.Infrastructure;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class ReconcilePaymentRefundTests : TestBase
{
    [Test]
    public async Task ShouldPersistAndReconcileAFullProviderRefund()
    {
        var organization = Organization.Create("Refund Test", $"refund-{Guid.NewGuid():N}", "PHP");
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
            "Refundable product",
            "REFUND-SKU",
            500m,
            1,
            0m,
            null,
            0m,
            null
        );
        order.MarkPaid(DateTimeOffset.UtcNow);
        var payment = Payment.Initiate(
            organization.Id,
            order.Id,
            "PayMongo",
            $"payment:{order.Id:N}",
            500m,
            "PHP"
        );
        payment.MarkPaid("pay_refund_test", DateTimeOffset.UtcNow);
        await AddAsync(organization);
        await AddAsync(order);
        await AddAsync(payment);
        await RunAsAdministratorAsync(organization.Id);

        var refundId = await SendAsync(
            new RequestPaymentRefundCommand(
                organization.Id,
                order.Id,
                500m,
                "requested_by_customer"
            )
        );
        (await FindAsync<PaymentRefund>(refundId))!.Status.ShouldBe(PaymentRefundStatus.Pending);

        GetRequiredService<TestPaymentGateway>().State = new PaymentProviderState(
            "pay_refund_test",
            "paid",
            50_000,
            "PHP",
            DateTimeOffset.UtcNow,
            50_000
        );
        (await SendAsync(new ReconcilePaymentCommand(organization.Id, payment.Id))).ShouldBeTrue();

        (await FindAsync<Payment>(payment.Id))!.Status.ShouldBe(PaymentStatus.Refunded);
        (await FindAsync<Order>(order.Id))!.Status.ShouldBe(OrderStatus.Refunded);
        (await FindAsync<PaymentRefund>(refundId))!.Status.ShouldBe(PaymentRefundStatus.Succeeded);
    }
}
