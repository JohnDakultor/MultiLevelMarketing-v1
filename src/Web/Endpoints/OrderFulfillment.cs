using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Commerce.Commands.AdminCancelOrder;
using modular_mlm.Application.Commerce.Commands.MarkOrderDelivered;
using modular_mlm.Application.Commerce.Commands.MarkOrderShipped;
using modular_mlm.Application.Commerce.Commands.StartOrderProcessing;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class OrderFulfillment : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/orders";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.RequireRateLimiting(RateLimitPolicyNames.AdministratorFinancialAction);
        group.MapPost(StartProcessing, "{orderId:guid}/processing");
        group.MapPost(MarkShipped, "{orderId:guid}/ship");
        group.MapPost(MarkDelivered, "{orderId:guid}/deliver");
        group.MapPost(Cancel, "{orderId:guid}/cancel");
    }

    public static async Task<NoContent> StartProcessing(ISender sender, Guid organizationId, Guid orderId)
    {
        await sender.Send(new StartOrderProcessingCommand(organizationId, orderId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> MarkShipped(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        MarkOrderShippedRequest request
    )
    {
        await sender.Send(new MarkOrderShippedCommand(
            organizationId, orderId, request.Carrier, request.TrackingNumber
        ));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> MarkDelivered(ISender sender, Guid organizationId, Guid orderId)
    {
        await sender.Send(new MarkOrderDeliveredCommand(organizationId, orderId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> Cancel(
        ISender sender,
        Guid organizationId,
        Guid orderId,
        AdminCancelOrderRequest request
    )
    {
        await sender.Send(new AdminCancelOrderCommand(organizationId, orderId, request.Reason));
        return TypedResults.NoContent();
    }
}

public sealed record MarkOrderShippedRequest(string? Carrier, string? TrackingNumber);
public sealed record AdminCancelOrderRequest(string Reason);
