using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Payments;

public sealed class PaymentTests
{
    [Test]
    public void ShouldTrackTheProviderCheckoutAndPaymentLifecycle()
    {
        var payment = Payment.Initiate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PayMongo",
            "payment-key",
            1_250m,
            "php"
        );
        var checkoutUrl = new Uri("https://checkout.paymongo.com/test");

        payment.AttachCheckoutSession("cs_test", checkoutUrl);
        payment.MarkPaid("pay_test", DateTimeOffset.UtcNow);
        payment.MarkCompensationProcessed(DateTimeOffset.UtcNow);

        payment.ProviderCheckoutSessionId.ShouldBe("cs_test");
        payment.ProviderPaymentId.ShouldBe("pay_test");
        payment.CheckoutUrl.ShouldBe(checkoutUrl);
        payment.Currency.ShouldBe("PHP");
        payment.Status.ShouldBe(PaymentStatus.Paid);
        payment.CompensationProcessedAt.ShouldNotBeNull();
    }

    [Test]
    public void ShouldReconcilePartialAndFullRefundAmounts()
    {
        var payment = Payment.Initiate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PayMongo",
            "payment-refund-key",
            1_000m,
            "PHP"
        );
        payment.MarkPaid("pay_refund", DateTimeOffset.UtcNow);

        payment.ReconcileRefundedAmount(250m);
        payment.Status.ShouldBe(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmount.ShouldBe(250m);

        payment.ReconcileRefundedAmount(1_000m);
        payment.Status.ShouldBe(PaymentStatus.Refunded);
        payment.RefundedAmount.ShouldBe(1_000m);
    }
}
