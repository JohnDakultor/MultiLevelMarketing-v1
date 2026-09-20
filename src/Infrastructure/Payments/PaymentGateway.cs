using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.Payments;

public sealed class PaymentGateway(
    HttpClient httpClient,
    IOptions<PayMongoOptions> options,
    TimeProvider clock
) : IPaymentGateway
{
    private readonly PayMongoOptions _options = options.Value;

    public async Task<PaymentProviderState> GetPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken
    )
    {
        EnsureSecretConfigured();
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"v1/payments/{Uri.EscapeDataString(providerPaymentId)}",
            null,
            cancellationToken
        );
        await EnsureSuccessAsync(response, "payment retrieval", cancellationToken);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(cancellationToken)
        );
        var data = document.RootElement.GetProperty("data");
        var attributes = data.GetProperty("attributes");
        return new PaymentProviderState(
            data.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("PayMongo payment ID is missing."),
            attributes.GetProperty("status").GetString() ?? "unknown",
            attributes.GetProperty("amount").GetInt64(),
            attributes.GetProperty("currency").GetString() ?? string.Empty,
            attributes.TryGetProperty("paid_at", out var paidAt)
            && paidAt.ValueKind == JsonValueKind.Number
                ? DateTimeOffset.FromUnixTimeSeconds(paidAt.GetInt64())
                : null,
            GetRefundedAmount(attributes)
        );
    }

    public async Task<PaymentRefundResult> RefundAsync(
        CreatePaymentRefundRequest request,
        CancellationToken cancellationToken
    )
    {
        EnsureSecretConfigured();
        using var content = JsonContent.Create(
            new
            {
                data = new
                {
                    attributes = new
                    {
                        amount = request.AmountInMinorUnits,
                        payment_id = request.ProviderPaymentId,
                        reason = request.Reason,
                    },
                },
            }
        );
        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            "refunds",
            content,
            cancellationToken,
            request.IdempotencyKey
        );
        await EnsureSuccessAsync(response, "refund creation", cancellationToken);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(cancellationToken)
        );
        var data = document.RootElement.GetProperty("data");
        return new PaymentRefundResult(
            data.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("PayMongo refund ID is missing."),
            data.GetProperty("attributes").GetProperty("status").GetString() ?? "pending"
        );
    }

    public async Task<PaymentCheckoutSession> CreateCheckoutAsync(
        CreatePaymentCheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        EnsureSecretConfigured();
        using var message = new HttpRequestMessage(HttpMethod.Post, "v2/checkout_sessions");
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.SecretKey}:"))
        );
        message.Headers.Add("Idempotency-Key", request.IdempotencyKey);
        message.Content = JsonContent.Create(
            new
            {
                data = new
                {
                    attributes = new
                    {
                        line_items = request.LineItems.Select(item => new
                        {
                            name = item.Name,
                            amount = item.AmountInMinorUnits,
                            currency = item.Currency,
                            quantity = item.Quantity,
                        }),
                        payment_method_types = request.PaymentMethodTypes,
                        success_url = request.SuccessUrl.AbsoluteUri,
                        cancel_url = request.CancelUrl.AbsoluteUri,
                        reference_number = request.OrderNumber,
                        description = $"Payment for order {request.OrderNumber}",
                        metadata = new Dictionary<string, string>
                        {
                            ["order_id"] = request.OrderId.ToString(),
                        },
                    },
                },
            }
        );

        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"PayMongo rejected checkout-session creation with HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode
            );

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(cancellationToken)
        );
        var data = document.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetString();
        var checkoutUrl = data.GetProperty("attributes").GetProperty("checkout_url").GetString();
        if (
            string.IsNullOrWhiteSpace(id)
            || !Uri.TryCreate(checkoutUrl, UriKind.Absolute, out var parsedCheckoutUrl)
        )
            throw new InvalidOperationException("PayMongo returned an invalid checkout session.");

        return new PaymentCheckoutSession(id, parsedCheckoutUrl);
    }

    public PaymentWebhookNotification? VerifyAndParseWebhook(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            throw new InvalidOperationException("PayMongo webhook secret is not configured.");
        if (!VerifyWebhookSignature(payload, signatureHeader))
            throw new UnauthorizedAccessException("The PayMongo webhook signature is invalid.");

        using var document = JsonDocument.Parse(payload);
        var eventData = document.RootElement.GetProperty("data");
        var eventId = eventData.GetProperty("id").GetString();
        var attributes = eventData.GetProperty("attributes");
        var eventType = attributes.GetProperty("type").GetString();
        if (
            eventType
            is not (
                "checkout_session.payment.paid"
                or "payment.refunded"
                or "payment.refund.updated"
            )
        )
            return null;

        var resource = attributes.GetProperty("data");
        var resourceAttributes = resource.GetProperty("attributes");
        var isCheckout = eventType == "checkout_session.payment.paid";
        var checkoutSessionId = isCheckout ? resource.GetProperty("id").GetString() : string.Empty;
        var referenceNumber =
            isCheckout && resourceAttributes.TryGetProperty("reference_number", out var reference)
                ? reference.GetString()
                : string.Empty;
        var paymentId =
            isCheckout ? TryGetPaymentId(resourceAttributes)
            : resourceAttributes.TryGetProperty("payment_id", out var paymentReference)
                ? paymentReference.GetString()
            : resource.GetProperty("id").GetString();
        var occurredAt = attributes.TryGetProperty("created_at", out var createdAt)
            ? DateTimeOffset.FromUnixTimeSeconds(createdAt.GetInt64())
            : clock.GetUtcNow();

        if (
            string.IsNullOrWhiteSpace(eventId)
            || string.IsNullOrWhiteSpace(eventType)
            || (isCheckout && string.IsNullOrWhiteSpace(checkoutSessionId))
            || (isCheckout && string.IsNullOrWhiteSpace(referenceNumber))
            || (!isCheckout && string.IsNullOrWhiteSpace(paymentId))
        )
            throw new InvalidOperationException("PayMongo returned an invalid webhook event.");

        return new PaymentWebhookNotification(
            eventId,
            eventType,
            checkoutSessionId ?? string.Empty,
            referenceNumber ?? string.Empty,
            paymentId,
            occurredAt,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant()
        );
    }

    public bool VerifyWebhookSignature(string payload, string signatureHeader)
    {
        var parts = signatureHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);
        if (!parts.TryGetValue("t", out var timestampText))
            return VerifyHexSignature(payload, signatureHeader);
        if (
            !long.TryParse(
                timestampText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var timestamp
            )
            || Math.Abs(
                (clock.GetUtcNow() - DateTimeOffset.FromUnixTimeSeconds(timestamp)).TotalMinutes
            ) > _options.WebhookToleranceMinutes
        )
            return false;

        var signatureName = _options.SecretKey.StartsWith("sk_live_", StringComparison.Ordinal)
            ? "li"
            : "te";
        return parts.TryGetValue(signatureName, out var signature)
            && VerifyHexSignature($"{timestampText}.{payload}", signature);
    }

    private bool VerifyHexSignature(string signedPayload, string suppliedSignature)
    {
        byte[] suppliedBytes;
        try
        {
            suppliedBytes = Convert.FromHexString(suppliedSignature);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.WebhookSecret),
            Encoding.UTF8.GetBytes(signedPayload)
        );
        return suppliedBytes.Length == expected.Length
            && CryptographicOperations.FixedTimeEquals(suppliedBytes, expected);
    }

    private static string? TryGetPaymentId(JsonElement attributes)
    {
        if (
            !attributes.TryGetProperty("payments", out var payments)
            || payments.ValueKind != JsonValueKind.Array
            || payments.GetArrayLength() == 0
        )
            return null;
        return payments[0].TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    private static long GetRefundedAmount(JsonElement attributes)
    {
        if (
            !attributes.TryGetProperty("refunds", out var refunds)
            || refunds.ValueKind != JsonValueKind.Array
        )
            return 0;
        return refunds
            .EnumerateArray()
            .Where(refund =>
                refund.TryGetProperty("attributes", out var refundAttributes)
                && refundAttributes.TryGetProperty("status", out var status)
                && string.Equals(
                    status.GetString(),
                    "succeeded",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Sum(refund => refund.GetProperty("attributes").GetProperty("amount").GetInt64());
    }

    private void EnsureSecretConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException("PayMongo secret key is not configured.");
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string uri,
        HttpContent? content,
        CancellationToken cancellationToken,
        string? idempotencyKey = null
    )
    {
        using var message = new HttpRequestMessage(method, uri) { Content = content };
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.SecretKey}:"))
        );
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            message.Headers.Add("Idempotency-Key", idempotencyKey);
        return await httpClient.SendAsync(message, cancellationToken);
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
