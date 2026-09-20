using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Catalog.Commands.AssignCommissionProfile;
using modular_mlm.Application.Catalog.Commands.CreateProduct;
using modular_mlm.Application.Catalog.Commands.CreateProductCommissionProfile;
using modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles;
using modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles.Models;
using modular_mlm.Application.Catalog.Queries.GetProducts;
using modular_mlm.Application.Catalog.Queries.GetProducts.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class Products : IEndpointGroup
{
    public static string? RoutePrefix => "/api/organizations/{organizationId:guid}/products";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetProducts).AllowAnonymous();
        group
            .MapPost(CreateProduct)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPost(CreateCommissionProfile, "commission-profiles")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapGet(GetCommissionProfiles, "commission-profiles")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPut(AssignCommissionProfile, "{productId:guid}/commission-profile")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public static async Task<Ok<IReadOnlyList<ProductDto>>> GetProducts(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20
    ) => TypedResults.Ok(await sender.Send(new GetProductsQuery(organizationId, page, pageSize)));

    public static async Task<Created<Guid>> CreateProduct(
        ISender sender,
        Guid organizationId,
        CreateProductRequest request
    )
    {
        var id = await sender.Send(
            new CreateProductCommand(
                organizationId,
                request.CategoryId,
                request.Name,
                request.Slug,
                request.Description,
                request.Sku,
                request.Price,
                request.BusinessVolume,
                request.StockQuantity
            )
        );
        return TypedResults.Created($"/api/organizations/{organizationId}/products/{id}", id);
    }

    public static async Task<Created<Guid>> CreateCommissionProfile(
        ISender sender,
        Guid organizationId,
        CreateProductCommissionProfileRequest request
    )
    {
        var id = await sender.Send(
            new CreateProductCommissionProfileCommand(
                organizationId,
                request.Name,
                request.DirectSalesEligible,
                request.DirectSalesRateOverride,
                request.BinaryVolumeEligible,
                request.BinaryVolumeOverride,
                request.EffectiveFrom
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/products/commission-profiles/{id}",
            id
        );
    }

    public static async Task<Ok<IReadOnlyList<ProductCommissionProfileDto>>> GetCommissionProfiles(
        ISender sender,
        Guid organizationId
    ) => TypedResults.Ok(await sender.Send(new GetProductCommissionProfilesQuery(organizationId)));

    public static async Task<NoContent> AssignCommissionProfile(
        ISender sender,
        Guid organizationId,
        Guid productId,
        AssignCommissionProfileRequest request
    )
    {
        await sender.Send(
            new AssignCommissionProfileCommand(
                organizationId,
                productId,
                request.CommissionProfileId
            )
        );
        return TypedResults.NoContent();
    }
}

public sealed record CreateProductRequest(
    Guid CategoryId,
    string Name,
    string Slug,
    string Description,
    string Sku,
    decimal Price,
    decimal BusinessVolume,
    int StockQuantity
);

public sealed record CreateProductCommissionProfileRequest(
    string Name,
    bool DirectSalesEligible,
    decimal? DirectSalesRateOverride,
    bool BinaryVolumeEligible,
    decimal? BinaryVolumeOverride,
    DateTimeOffset EffectiveFrom
);

public sealed record AssignCommissionProfileRequest(Guid? CommissionProfileId);
