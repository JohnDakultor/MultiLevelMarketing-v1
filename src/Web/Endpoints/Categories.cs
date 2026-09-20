using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Catalog.Queries.GetCategories;
using modular_mlm.Application.Catalog.Queries.GetCategories.Models;

namespace modular_mlm.Web.Endpoints;

public sealed class Categories : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/categories";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetCategories).AllowAnonymous();
    }

    public static async Task<Ok<IReadOnlyList<CategoryDto>>> GetCategories(
        ISender sender,
        Guid organizationId
    ) => TypedResults.Ok(await sender.Send(new GetCategoriesQuery(organizationId)));
}
