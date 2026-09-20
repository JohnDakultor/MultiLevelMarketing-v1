using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Commerce.Commands.HandlePaymentWebhook;
using modular_mlm.Application.Commerce.Commands.ReconcilePaymentByProvider;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Payouts.Commands.ReconcilePayoutByTransfer;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class PayMongoWebhooks : IEndpointGroup
{
    public static string? RoutePrefix => "/api/webhooks/paymongo";

    public static void Map(RouteGroupBuilder group)
    {
        group.ApplyWebhookLimits();
        group.MapPost(Receive, string.Empty).AllowAnonymous();
        group.MapPost(ReceiveTransfer, "transfers").AllowAnonymous();
    }

    public static async Task<
        Results<Ok, UnauthorizedHttpResult, BadRequest, NotFound>
    > ReceiveTransfer(
        HttpRequest request,
        IPaymentGateway gateway,
        IWebhookFailureRecorder failureRecorder,
        ISender sender,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var signature =
                request.Headers["Paymongo-Signature"].FirstOrDefault()
                ?? request.Headers["X-Paymongo-Signature"].FirstOrDefault();
            if (
                string.IsNullOrWhiteSpace(signature)
                || !gateway.VerifyWebhookSignature(payload, signature)
            )
                return TypedResults.Unauthorized();
            using var document = JsonDocument.Parse(payload);
            var data = document.RootElement.GetProperty("data");
            var transferId = data.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(transferId))
                return TypedResults.BadRequest();
            try
            {
                await sender.Send(
                    new ReconcilePayoutByTransferCommand(transferId),
                    cancellationToken
                );
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await failureRecorder.RecordAsync(
                    "PayMongo",
                    transferId,
                    "transfer.updated",
                    transferId,
                    "Payout",
                    Convert
                        .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))
                        .ToLowerInvariant(),
                    exception.Message,
                    cancellationToken
                );
                throw;
            }
            return TypedResults.Ok();
        }
        catch (JsonException)
        {
            return TypedResults.BadRequest();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    public static async Task<Results<Ok, UnauthorizedHttpResult, BadRequest>> Receive(
        HttpRequest request,
        IPaymentGateway gateway,
        IWebhookFailureRecorder failureRecorder,
        ISender sender,
        CancellationToken cancellationToken
    )
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature =
            request.Headers["Paymongo-Signature"].FirstOrDefault()
            ?? request.Headers["X-Paymongo-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(signature))
            return TypedResults.Unauthorized();

        PaymentWebhookNotification? notification;
        try
        {
            notification = gateway.VerifyAndParseWebhook(payload, signature);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (JsonException)
        {
            return TypedResults.BadRequest();
        }

        if (notification is null)
            return TypedResults.Ok();

        if (notification.EventType is "payment.refunded" or "payment.refund.updated")
        {
            try
            {
                await sender.Send(
                    new ReconcilePaymentByProviderCommand(notification.PaymentId!),
                    cancellationToken
                );
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await failureRecorder.RecordAsync(
                    "PayMongo",
                    notification.EventId,
                    notification.EventType,
                    notification.PaymentId!,
                    "Payment",
                    notification.PayloadHash,
                    exception.Message,
                    cancellationToken
                );
                throw;
            }
            return TypedResults.Ok();
        }

        await sender.Send(
            new HandlePaymentWebhookCommand(
                notification.EventId,
                notification.EventType,
                notification.CheckoutSessionId,
                notification.ReferenceNumber,
                notification.PaymentId,
                notification.OccurredAt,
                notification.PayloadHash
            ),
            cancellationToken
        );
        return TypedResults.Ok();
    }
}
