using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Catalog.Commands.ArchiveProductVariant;
using modular_mlm.Application.Catalog.Commands.CreateProductVariant;
using modular_mlm.Application.Catalog.Commands.UpdateProductVariant;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class ProductVariantAdministration : IEndpointGroup
{
    public static string RoutePrefix =>
        "/api/organizations/{organizationId:guid}/admin/products/{productId:guid}/variants";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapPost(Create);
        group.MapPut(Update, "{productVariantId:guid}");
        group.MapPost(Archive, "{productVariantId:guid}/archive");
    }

    public static async Task<Created<Guid>> Create(
        ISender sender,
        Guid organizationId,
        Guid productId,
        CreateProductVariantRequest request
    )
    {
        var id = await sender.Send(
            new CreateProductVariantCommand(
                organizationId,
                productId,
                request.Sku,
                request.Price,
                request.BusinessVolume,
                request.InitialOnHandQuantity,
                request.StockKeepingEnabled
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/admin/products/{productId}/variants/{id}",
            id
        );
    }

    public static async Task<Ok<Guid>> Update(
        ISender sender,
        Guid organizationId,
        Guid productId,
        Guid productVariantId,
        UpdateProductVariantRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new UpdateProductVariantCommand(
                    organizationId,
                    productId,
                    productVariantId,
                    request.Price,
                    request.BusinessVolume,
                    request.Weight,
                    request.AttributesJson,
                    request.StockKeepingEnabled,
                    request.ExpectedVersion
                )
            )
        );

    public static async Task<Ok<Guid>> Archive(
        ISender sender,
        Guid organizationId,
        Guid productId,
        Guid productVariantId,
        ArchiveProductVariantRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new ArchiveProductVariantCommand(
                    organizationId,
                    productId,
                    productVariantId,
                    request.Reason,
                    request.ExpectedVersion
                )
            )
        );
}

public sealed record CreateProductVariantRequest(
    string Sku,
    decimal Price,
    decimal BusinessVolume,
    int InitialOnHandQuantity,
    bool StockKeepingEnabled
);

public sealed record UpdateProductVariantRequest(
    decimal Price,
    decimal BusinessVolume,
    decimal? Weight,
    string AttributesJson,
    bool StockKeepingEnabled,
    long ExpectedVersion
);

public sealed record ArchiveProductVariantRequest(string Reason, int ExpectedVersion);
