using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Commerce.Commands.CreateCheckout;
using modular_mlm.Application.Commerce.Commands.CreatePaymentSession;
using modular_mlm.Application.Commerce.Commands.CreatePaymentSession.Models;
using modular_mlm.Application.Commerce.Commands.ReconcilePayment;
using modular_mlm.Application.Commerce.Commands.RequestPaymentRefund;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class Orders : IEndpointGroup
{
    public static string? RoutePrefix => "/api/organizations/{organizationId:guid}/orders";

    public static void Map(RouteGroupBuilder group)
    {
        group
            .MapPost(CreateCheckout, "checkout")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.Checkout);
        group
            .MapPost(CreatePaymentSession, "{orderId:guid}/payment-session")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.Payment);
        group
            .MapPost(RequestRefund, "{orderId:guid}/refunds")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator))
            .RequireRateLimiting(RateLimitPolicyNames.Refund);
        group
            .MapPost(ReconcilePayment, "payments/{paymentId:guid}/reconcile")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator))
            .RequireRateLimiting(RateLimitPolicyNames.AdministratorFinancialAction);
    }

    public static async Task<Ok<PaymentSessionDto>> CreatePaymentSession(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        CreatePaymentSessionRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new CreatePaymentSessionCommand(
                    organizationId,
                    orderId,
                    request.SuccessUrl,
                    request.CancelUrl,
                    request.PaymentMethodTypes
                )
            )
        );

    public static async Task<Created<Guid>> CreateCheckout(
        ISender sender,
        Guid organizationId,
        CreateCheckoutRequest request
    )
    {
        var id = await sender.Send(
            new CreateCheckoutCommand(
                organizationId,
                request.ShippingAddress,
                request.BillingAddress
            )
        );
        return TypedResults.Created($"/api/organizations/{organizationId}/orders/{id}", id);
    }

    public static async Task<Created<Guid>> RequestRefund(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        RequestPaymentRefundRequest request
    )
    {
        var id = await sender.Send(
            new RequestPaymentRefundCommand(organizationId, orderId, request.Amount, request.Reason)
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/orders/{orderId}/refunds/{id}",
            id
        );
    }

    public static async Task<Ok<bool>> ReconcilePayment(
        ISender sender,
        Guid organizationId,
        Guid paymentId
    ) =>
        TypedResults.Ok(
            await sender.Send(new ReconcilePaymentForOrganizationCommand(organizationId, paymentId))
        );
}

public sealed record CreateCheckoutRequest(
    CheckoutAddressInput ShippingAddress,
    CheckoutAddressInput BillingAddress
);

public sealed record CreatePaymentSessionRequest(
    Uri SuccessUrl,
    Uri CancelUrl,
    IReadOnlyList<string> PaymentMethodTypes
);

public sealed record RequestPaymentRefundRequest(decimal Amount, string Reason);
