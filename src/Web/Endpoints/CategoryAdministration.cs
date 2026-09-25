using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Catalog.Commands.ActivateCategory;
using modular_mlm.Application.Catalog.Commands.ArchiveCategory;
using modular_mlm.Application.Catalog.Commands.CreateCategory;
using modular_mlm.Application.Catalog.Commands.RenameCategory;
using modular_mlm.Application.Catalog.Queries.GetAdminCategories;
using modular_mlm.Application.Catalog.Queries.GetAdminCategories.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class CategoryAdministration : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/categories";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetCategories);
        group.MapPost(CreateCategory);
        group.MapPut(RenameCategory, "{categoryId:guid}");
        group.MapPost(ArchiveCategory, "{categoryId:guid}/archive");
        group.MapPost(ActivateCategory, "{categoryId:guid}/activate");
    }

    public static async Task<Ok<AdminCategoriesPageDto>> GetCategories(
        ISender sender,
        Guid organizationId,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 20
    ) => TypedResults.Ok(await sender.Send(
        new GetAdminCategoriesQuery(organizationId, includeInactive, page, pageSize)
    ));

    public static async Task<Created<Guid>> CreateCategory(
        ISender sender,
        Guid organizationId,
        CreateCategoryRequest request
    )
    {
        var id = await sender.Send(new CreateCategoryCommand(organizationId, request.Name, request.Slug));
        return TypedResults.Created($"/api/organizations/{organizationId}/admin/categories/{id}", id);
    }

    public static async Task<NoContent> RenameCategory(
        ISender sender,
        Guid organizationId,
        Guid categoryId,
        RenameCategoryRequest request
    )
    {
        await sender.Send(new RenameCategoryCommand(organizationId, categoryId, request.Name));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> ArchiveCategory(ISender sender, Guid organizationId, Guid categoryId)
    {
        await sender.Send(new ArchiveCategoryCommand(organizationId, categoryId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> ActivateCategory(ISender sender, Guid organizationId, Guid categoryId)
    {
        await sender.Send(new ActivateCategoryCommand(organizationId, categoryId));
        return TypedResults.NoContent();
    }
}

public sealed record CreateCategoryRequest(string Name, string Slug);
public sealed record RenameCategoryRequest(string Name);
