using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Payments;
using modular_mlm.Infrastructure.Payouts;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Payouts;

public sealed class PayoutProviderTests
{
    [Test]
    public async Task ShouldCreateASinglePayMongoBatchTransfer()
    {
        var handler = new RecordingHandler(
            """
            {"data":{"id":"batch_test","transfers":[{"id":"transfer_test","status":"pending","batch_transfer_id":"batch_test"}]}}
            """
        );
        var provider = CreateProvider(handler);

        var result = await provider.CreateAsync(
            new CreateProviderPayoutRequest(
                Guid.NewGuid(),
                50_000,
                "PHP",
                new PayoutDestination("Juan Dela Cruz", "09171234567", "GXCHPHM2XXX", "instapay"),
                "payout-key"
            ),
            CancellationToken.None
        );

        result.TransferId.ShouldBe("transfer_test");
        handler.Request!.RequestUri.ShouldBe(
            new Uri("https://api.paymongo.com/v2/batch_transfers")
        );
        handler.Body.ShouldContain("\"amount\":50000");
        handler.Body.ShouldContain("\"bic\":\"GXCHPHM2XXX\"");
    }

    [Test]
    public async Task ShouldRetrieveTransferStatus()
    {
        var handler = new RecordingHandler(
            """
            {"data":{"id":"transfer_test","attributes":{"status":"succeeded","batch_transfer_id":"batch_test","provider_reference_number":"provider_ref"}}}
            """
        );
        var provider = CreateProvider(handler);

        var result = await provider.GetStatusAsync("transfer_test", CancellationToken.None);

        result.Status.ShouldBe("succeeded");
        result.ProviderReference.ShouldBe("provider_ref");
        handler.Request!.RequestUri.ShouldBe(
            new Uri("https://api.paymongo.com/v2/transfers/transfer_test")
        );
    }

    private static PayoutProvider CreateProvider(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.paymongo.com/") },
            Options.Create(
                new PayMongoOptions
                {
                    SecretKey = "sk_test_example",
                    WalletAccountNumber = "0000000001",
                    WalletAccountName = "Test Merchant",
                }
            )
        );

    private sealed class RecordingHandler(string response) : HttpMessageHandler
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
                Content = new StringContent(response, Encoding.UTF8, "application/json"),
            };
        }
    }
}
