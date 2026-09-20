using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Organizations.Commands.UploadBrandingAsset;
using modular_mlm.Application.Organizations.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class OrganizationMedia : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations";

    public static void Map(RouteGroupBuilder group)
    {
        group
            .MapPost(UploadBrandingAsset, "{organizationId:guid}/admin/branding/assets/{assetKind}")
            .RequireAuthorization(policy =>
                policy.RequireRole(Roles.Administrator, Roles.PlatformAdministrator)
            )
            .DisableAntiforgery()
            .WithMetadata(
                new RequestSizeLimitAttribute(
                    BrandingAssetLimits.MaximumContentLength + (64 * 1024)
                )
            );
    }

    public static async Task<Ok<StoredObject>> UploadBrandingAsset(
        ISender sender,
        Guid organizationId,
        BrandingAssetKind assetKind,
        IFormFile file
    )
    {
        await using var content = file.OpenReadStream();
        var stored = await sender.Send(
            new UploadBrandingAssetCommand(
                organizationId,
                assetKind,
                file.FileName,
                file.ContentType,
                file.Length,
                content
            )
        );
        return TypedResults.Ok(stored);
    }
}
