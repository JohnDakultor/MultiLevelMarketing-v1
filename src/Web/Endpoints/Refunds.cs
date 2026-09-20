using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Commerce.Commands.ReconcileItemRefund;
using modular_mlm.Application.Commerce.Commands.RequestItemRefund;
using modular_mlm.Application.Commerce.Queries.GetRefundHistory;
using modular_mlm.Application.Commerce.Queries.GetRefundHistory.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class Refunds : IEndpointGroup
{
    public static string RoutePrefix =>
        "/api/organizations/{organizationId:guid}/orders/{orderId:guid}/refunds";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.RequireRateLimiting(RateLimitPolicyNames.Refund);
        group.MapPost(RequestItemRefund, "items/{orderItemId:guid}");
        group.MapGet(GetRefundHistory);
        group.MapPost(ReconcileItemRefund, "{refundId:guid}/reconcile");
    }

    public static async Task<Created<Guid>> RequestItemRefund(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        Guid orderItemId,
        RequestItemRefundRequest request
    )
    {
        var id = await sender.Send(
            new RequestItemRefundCommand(
                organizationId,
                orderId,
                orderItemId,
                request.Quantity,
                request.Reason
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/orders/{orderId}/refunds/{id}",
            id
        );
    }

    public static async Task<Ok<IReadOnlyList<RefundHistoryItemDto>>> GetRefundHistory(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        int page = 1,
        int pageSize = 20
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetRefundHistoryQuery(organizationId, orderId, page, pageSize))
        );

    public static async Task<Ok<bool>> ReconcileItemRefund(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        Guid refundId
    ) =>
        TypedResults.Ok(
            await sender.Send(new ReconcileItemRefundCommand(organizationId, refundId))
        );
}

public sealed record RequestItemRefundRequest(decimal Quantity, string Reason);
