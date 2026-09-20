using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Payments;

namespace modular_mlm.Infrastructure.Payouts;

public sealed class PayoutProvider(HttpClient httpClient, IOptions<PayMongoOptions> options)
    : IPayoutProvider
{
    private readonly PayMongoOptions _options = options.Value;

    public async Task<ProviderPayoutResult> CreateAsync(
        CreateProviderPayoutRequest request,
        CancellationToken cancellationToken
    )
    {
        EnsureConfigured();
        using var message = CreateRequest(HttpMethod.Post, "v2/batch_transfers");
        message.Headers.Add("Idempotency-Key", request.IdempotencyKey);
        message.Content = JsonContent.Create(
            new
            {
                transfers = new[]
                {
                    new
                    {
                        provider = request.Destination.Rail,
                        amount = request.AmountInMinorUnits,
                        currency = request.Currency,
                        purpose = "Commission payout",
                        description = $"Agent commission payout {request.PayoutRequestId:D}",
                        reference_number = request.PayoutRequestId.ToString("N"),
                        source_account = new
                        {
                            number = _options.WalletAccountNumber,
                            name = _options.WalletAccountName,
                            bic = _options.WalletBic,
                        },
                        destination_account = new
                        {
                            number = request.Destination.AccountNumber,
                            name = request.Destination.AccountName,
                            bic = request.Destination.BankCode,
                        },
                        callback_url = _options.PayoutCallbackUrl.AbsoluteUri,
                        metadata = new Dictionary<string, string>
                        {
                            ["payout_request_id"] = request.PayoutRequestId.ToString("D"),
                        },
                    },
                },
            }
        );
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, "payout creation", cancellationToken);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(cancellationToken)
        );
        return ParseBatch(document.RootElement);
    }

    public async Task<ProviderPayoutResult> GetStatusAsync(
        string transferId,
        CancellationToken cancellationToken
    )
    {
        EnsureConfigured();
        using var message = CreateRequest(
            HttpMethod.Get,
            $"v2/transfers/{Uri.EscapeDataString(transferId)}"
        );
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, "payout status retrieval", cancellationToken);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(cancellationToken)
        );
        return ParseTransfer(document.RootElement.GetProperty("data"));
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.SecretKey}:"))
        );
        return message;
    }

    private static ProviderPayoutResult ParseBatch(JsonElement root)
    {
        var data = root.GetProperty("data");
        var batchId = data.GetProperty("id").GetString() ?? string.Empty;
        var transfer = data.GetProperty("transfers")[0];
        return ParseTransfer(transfer) with { BatchId = batchId };
    }

    private static ProviderPayoutResult ParseTransfer(JsonElement transfer)
    {
        var attributes = transfer.TryGetProperty("attributes", out var nested) ? nested : transfer;
        return new ProviderPayoutResult(
            GetString(attributes, "batch_transfer_id") ?? string.Empty,
            transfer.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("PayMongo transfer ID is missing."),
            GetString(attributes, "status") ?? "pending",
            GetString(attributes, "provider_reference_number"),
            GetString(attributes, "failure_code") ?? GetString(attributes, "provider_error_code"),
            GetString(attributes, "failure_message") ?? GetString(attributes, "provider_error")
        );
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private void EnsureConfigured()
    {
        if (
            string.IsNullOrWhiteSpace(_options.SecretKey)
            || string.IsNullOrWhiteSpace(_options.WalletAccountNumber)
            || string.IsNullOrWhiteSpace(_options.WalletAccountName)
        )
            throw new InvalidOperationException(
                "PayMongo secret key and wallet source account must be configured through secrets."
            );
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken
    )
    {
        if (response.IsSuccessStatusCode)
            return;
        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"PayMongo rejected {operation} with HTTP {(int)response.StatusCode}: {details}",
            null,
            response.StatusCode
        );
    }
}
