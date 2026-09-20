using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Catalog.Commands.ArchiveProduct;
using modular_mlm.Application.Catalog.Commands.PublishProduct;
using modular_mlm.Application.Catalog.Commands.UpdateProduct;
using modular_mlm.Application.Catalog.Queries.GetAdminCategories;
using modular_mlm.Application.Catalog.Queries.GetAdminCategories.Models;
using modular_mlm.Application.Catalog.Queries.GetAdminProduct;
using modular_mlm.Application.Catalog.Queries.GetAdminProduct.Models;
using modular_mlm.Application.Catalog.Queries.GetAdminProducts;
using modular_mlm.Application.Catalog.Queries.GetAdminProducts.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class ProductAdministration : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/products";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetAdminProducts);
        group.MapGet(GetAdminProduct, "{productId:guid}");
        group.MapGet(GetAdminCategories, "categories");
        group.MapPut(UpdateProduct, "{productId:guid}");
        group.MapPost(PublishProduct, "{productId:guid}/publish");
        group.MapPost(ArchiveProduct, "{productId:guid}/archive");
    }

    public static async Task<Ok<AdminProductDetailsDto>> GetAdminProduct(
        ISender sender,
        Guid organizationId,
        Guid productId
    ) => TypedResults.Ok(await sender.Send(new GetAdminProductQuery(organizationId, productId)));

    public static async Task<Ok<AdminCategoriesPageDto>> GetAdminCategories(
        ISender sender,
        Guid organizationId,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 20
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAdminCategoriesQuery(organizationId, includeInactive, page, pageSize)
            )
        );

    public static async Task<Ok<IReadOnlyList<AdminProductDto>>> GetAdminProducts(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 100
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetAdminProductsQuery(organizationId, page, pageSize))
        );

    public static async Task<NoContent> PublishProduct(
        ISender sender,
        Guid organizationId,
        Guid productId
    )
    {
        await sender.Send(new PublishProductCommand(organizationId, productId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> UpdateProduct(
        ISender sender,
        Guid organizationId,
        Guid productId,
        UpdateProductRequest request
    )
    {
        await sender.Send(
            new UpdateProductCommand(
                organizationId,
                productId,
                request.CategoryId,
                request.Name,
                request.Description
            )
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> ArchiveProduct(
        ISender sender,
        Guid organizationId,
        Guid productId
    )
    {
        await sender.Send(new ArchiveProductCommand(organizationId, productId));
        return TypedResults.NoContent();
    }
}

public sealed record UpdateProductRequest(Guid CategoryId, string Name, string Description);
