using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Catalog.Queries.GetProductBySlug;
using modular_mlm.Application.Catalog.Queries.GetProductBySlug.Models;

namespace modular_mlm.Web.Endpoints;

public sealed class ProductDetails : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/products";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetProductBySlug, "{slug}").AllowAnonymous();
    }

    public static async Task<Ok<ProductDetailDto>> GetProductBySlug(
        ISender sender,
        Guid organizationId,
        string slug,
        CancellationToken cancellationToken
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetProductBySlugQuery(organizationId, slug), cancellationToken)
        );
}
