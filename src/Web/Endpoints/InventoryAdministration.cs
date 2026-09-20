using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using modular_mlm.Application.Catalog.Commands.AdjustInventory;
using modular_mlm.Application.Inventory.Queries.GetInventory;
using modular_mlm.Application.Inventory.Queries.GetInventory.Models;
using modular_mlm.Application.Inventory.Queries.GetInventoryHistory;
using modular_mlm.Application.Inventory.Queries.GetInventoryHistory.Models;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class InventoryAdministration : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/inventory";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetInventory);
        group.MapGet(GetHistory, "{productVariantId:guid}/history");
        group
            .MapPost(Adjust, "{productVariantId:guid}/adjustments")
            .RequireRateLimiting("financial");
    }

    public static async Task<Ok<InventoryPageDto>> GetInventory(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        ProductStatus? status = null,
        bool lowStockOnly = false
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetInventoryQuery(organizationId, page, pageSize, search, status, lowStockOnly)
            )
        );

    public static async Task<Ok<InventoryHistoryPageDto>> GetHistory(
        ISender sender,
        Guid organizationId,
        Guid productVariantId,
        int page = 1,
        int pageSize = 20
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetInventoryHistoryQuery(organizationId, productVariantId, page, pageSize)
            )
        );

    public static async Task<Created<Guid>> Adjust(
        ISender sender,
        Guid organizationId,
        Guid productVariantId,
        AdjustInventoryRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey
    )
    {
        var id = await sender.Send(
            new AdjustInventoryCommand(
                organizationId,
                productVariantId,
                request.QuantityDelta,
                request.AdjustmentType,
                request.Reason,
                idempotencyKey,
                request.ExpectedVersion
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/admin/inventory/{productVariantId}/history",
            id
        );
    }
}

public sealed record AdjustInventoryRequest(
    int QuantityDelta,
    InventoryAdjustmentType AdjustmentType,
    string Reason,
    int ExpectedVersion
);
