using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Commerce.Commands.RequestCancellation;
using modular_mlm.Application.Commerce.Commands.RequestRefund;
using modular_mlm.Application.Commerce.Queries.GetMyOrders;
using modular_mlm.Application.Commerce.Queries.GetOrderDetails;
using modular_mlm.Application.Commerce.Queries.GetOrderDetails.Models;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Web.Endpoints;

public sealed class CustomerOrders : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/me/orders";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
        group.MapGet(GetMyOrders);
        group.MapGet(GetOrderDetails, "{orderId:guid}");
        group.MapPost(RequestCancellation, "{orderId:guid}/cancellation");
        group
            .MapPost(RequestCustomerRefund, "{orderId:guid}/items/{orderItemId:guid}/refunds")
            .RequireRateLimiting(Infrastructure.Security.RateLimitPolicyNames.Refund);
    }

    public static async Task<Ok<OrderSummariesPageDto>> GetMyOrders(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        OrderStatus? status = null
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetMyOrdersQuery(organizationId, page, pageSize, status))
        );

    public static async Task<Ok<OrderDetailsDto>> GetOrderDetails(
        ISender sender,
        Guid organizationId,
        Guid orderId
    ) => TypedResults.Ok(await sender.Send(new GetOrderDetailsQuery(organizationId, orderId)));

    public static async Task<NoContent> RequestCancellation(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        RequestCancellationRequest request
    )
    {
        await sender.Send(new RequestCancellationCommand(organizationId, orderId, request.Reason));
        return TypedResults.NoContent();
    }

    public static async Task<Created<Guid>> RequestCustomerRefund(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        Guid orderItemId,
        RequestCustomerRefundRequest request
    )
    {
        var refundId = await sender.Send(
            new RequestRefundCommand(
                organizationId,
                orderId,
                orderItemId,
                request.Quantity,
                request.Reason
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/me/orders/{orderId}/refunds/{refundId}",
            refundId
        );
    }
}

public sealed record RequestCancellationRequest(string Reason);

public sealed record RequestCustomerRefundRequest(int Quantity, string Reason);
