using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary;
using modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary.Models;
using modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails;
using modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails.Models;
using modular_mlm.Application.Commerce.Queries.GetAttributedOrders;
using modular_mlm.Application.Commerce.Queries.GetAttributedOrders.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class AgentSales : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/agent/sales";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Agent));
        group.MapGet(GetOrders);
        group.MapGet(GetOrderDetails, "{orderId:guid}");
        group.MapGet(GetProducts, "products");
    }

    public static async Task<Ok<AttributedOrdersPageDto>> GetOrders(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        OrderStatus? status = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAttributedOrdersQuery(organizationId, page, pageSize, status, from, to)
            )
        );

    public static async Task<Results<Ok<AttributedOrderDetailsDto>, NotFound>> GetOrderDetails(
        ISender sender,
        Guid organizationId,
        Guid orderId
    )
    {
        var details = await sender.Send(
            new GetAttributedOrderDetailsQuery(organizationId, orderId)
        );
        return details is null ? TypedResults.NotFound() : TypedResults.Ok(details);
    }

    public static async Task<Ok<AgentProductSalesPageDto>> GetProducts(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAgentProductSalesSummaryQuery(organizationId, page, pageSize, from, to)
            )
        );
}
