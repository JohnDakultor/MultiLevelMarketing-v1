using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails;
using modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails.Models;
using modular_mlm.Application.Commerce.Queries.GetAdminOrders;
using modular_mlm.Application.Commerce.Queries.GetAdminOrders.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class AdminOrders : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/orders";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetOrders);
        group.MapGet(GetOrderDetails, "{orderId:guid}");
    }

    public static async Task<Ok<AdminOrdersPageDto>> GetOrders(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        OrderStatus? status = null,
        PaymentStatus? paymentStatus = null,
        DateTimeOffset? createdFrom = null,
        DateTimeOffset? createdTo = null
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAdminOrdersQuery(
                    organizationId,
                    page,
                    pageSize,
                    search,
                    status,
                    paymentStatus,
                    createdFrom,
                    createdTo
                )
            )
        );

    public static async Task<Results<Ok<AdminOrderDetailsDto>, NotFound>> GetOrderDetails(
        ISender sender,
        Guid organizationId,
        Guid orderId
    )
    {
        var details = await sender.Send(new GetAdminOrderDetailsQuery(organizationId, orderId));
        return details is null ? TypedResults.NotFound() : TypedResults.Ok(details);
    }
}
