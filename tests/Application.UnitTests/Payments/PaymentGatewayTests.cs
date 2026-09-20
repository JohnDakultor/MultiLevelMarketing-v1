using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Payments;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Payments;

public sealed class PaymentGatewayTests
{
    [Test]
    public async Task ShouldCreateAVersionTwoHostedCheckoutSession()
    {
        var handler = new RecordingHttpMessageHandler(
            """
            {"data":{"id":"cs_created","attributes":{"checkout_url":"https://checkout.paymongo.com/cs_created"}}}
            """
        );
        var gateway = CreateGateway(DateTimeOffset.UtcNow, handler);

        var session = await gateway.CreateCheckoutAsync(
            new CreatePaymentCheckoutRequest(
                Guid.NewGuid(),
                "ORD-001",
                [new PaymentCheckoutLineItem("Product", 10_000, "PHP", 1)],
                ["card", "gcash", "qrph"],
                new Uri("https://store.example/success"),
                new Uri("https://store.example/cancel"),
                "checkout-idempotency-key"
            ),
            CancellationToken.None
        );

        session.Id.ShouldBe("cs_created");
        session.CheckoutUrl.ShouldBe(new Uri("https://checkout.paymongo.com/cs_created"));
        handler.Request.ShouldNotBeNull();
        handler.Request.RequestUri.ShouldBe(
            new Uri("https://api.paymongo.com/v2/checkout_sessions")
        );
        handler.Request.Headers.Authorization!.Scheme.ShouldBe("Basic");
        handler
            .Request.Headers.GetValues("Idempotency-Key")
            .Single()
            .ShouldBe("checkout-idempotency-key");
        handler.Body.ShouldContain("\"reference_number\":\"ORD-001\"");
        handler.Body.ShouldContain("\"amount\":10000");
    }

    [Test]
    public void ShouldVerifyAndParseATestModeCheckoutWebhook()
    {
        var now = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        const string payload = """
            {"data":{"id":"evt_test","attributes":{"type":"checkout_session.payment.paid","created_at":1787486400,"data":{"id":"cs_test","attributes":{"reference_number":"ORD-001","payments":[{"id":"pay_test"}]}}}}}
            """;
        var timestamp = now.ToUnixTimeSeconds().ToString();
        var signature = Convert
            .ToHexString(
                HMACSHA256.HashData(
                    Encoding.UTF8.GetBytes("whsk_test"),
                    Encoding.UTF8.GetBytes($"{timestamp}.{payload}")
                )
            )
            .ToLowerInvariant();
        var gateway = CreateGateway(now);

        var notification = gateway.VerifyAndParseWebhook(
            payload,
            $"t={timestamp},te={signature},li="
        );

        notification.ShouldNotBeNull();
        notification.EventId.ShouldBe("evt_test");
        notification.CheckoutSessionId.ShouldBe("cs_test");
        notification.ReferenceNumber.ShouldBe("ORD-001");
        notification.PaymentId.ShouldBe("pay_test");
    }

    [Test]
    public void ShouldRejectAnInvalidWebhookSignature()
    {
        var now = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var gateway = CreateGateway(now);

        Should.Throw<UnauthorizedAccessException>(() =>
            gateway.VerifyAndParseWebhook(
                "{}",
                $"t={now.ToUnixTimeSeconds()},te={new string('0', 64)},li="
            )
        );
    }

    [Test]
    public async Task ShouldRetrieveAndReconcileAPayment()
    {
        var handler = new RecordingHttpMessageHandler(
            """
            {"data":{"id":"pay_test","attributes":{"status":"paid","amount":10000,"currency":"PHP","paid_at":1787486400,"refunds":[{"attributes":{"status":"succeeded","amount":2500}}]}}}
            """
        );
        var gateway = CreateGateway(DateTimeOffset.UtcNow, handler);

        var state = await gateway.GetPaymentAsync("pay_test", CancellationToken.None);

        state.Status.ShouldBe("paid");
        state.AmountInMinorUnits.ShouldBe(10_000);
        state.RefundedAmountInMinorUnits.ShouldBe(2_500);
        handler.Request!.RequestUri.ShouldBe(
            new Uri("https://api.paymongo.com/v1/payments/pay_test")
        );
    }

    [Test]
    public async Task ShouldCreateAnIdempotentRefund()
    {
        var handler = new RecordingHttpMessageHandler(
            """
            {"data":{"id":"refund_test","attributes":{"status":"pending"}}}
            """
        );
        var gateway = CreateGateway(DateTimeOffset.UtcNow, handler);

        var result = await gateway.RefundAsync(
            new CreatePaymentRefundRequest(
                "pay_test",
                10_000,
                "requested_by_customer",
                "refund-key"
            ),
            CancellationToken.None
        );

        result.ProviderRefundId.ShouldBe("refund_test");
        handler.Request!.RequestUri.ShouldBe(new Uri("https://api.paymongo.com/refunds"));
        handler.Request.Headers.GetValues("Idempotency-Key").Single().ShouldBe("refund-key");
        handler.Body.ShouldContain("\"payment_id\":\"pay_test\"");
    }

    private static PaymentGateway CreateGateway(
        DateTimeOffset now,
        HttpMessageHandler? handler = null
    ) =>
        new(
            new HttpClient(handler ?? new EmptyHttpMessageHandler())
            {
                BaseAddress = new Uri("https://api.paymongo.com/"),
            },
            Options.Create(
                new PayMongoOptions { SecretKey = "sk_test_example", WebhookSecret = "whsk_test" }
            ),
            new FixedTimeProvider(now)
        );

    private sealed class EmptyHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
    }

    private sealed class RecordingHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Request = request;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
