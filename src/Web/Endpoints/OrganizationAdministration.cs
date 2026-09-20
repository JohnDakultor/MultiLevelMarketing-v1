using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Organizations.Commands.ConfigureOrganizationDomain;
using modular_mlm.Application.Organizations.Commands.CreateOrganization;
using modular_mlm.Application.Organizations.Commands.PublishBranding;
using modular_mlm.Application.Organizations.Commands.RemoveOrganizationDomain;
using modular_mlm.Application.Organizations.Commands.UpdateCommerceSettings;
using modular_mlm.Application.Organizations.Commands.UpdateOrganizationProfile;
using modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings;
using modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;
using modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Contracts.Organizations;
using modular_mlm.Web.Infrastructure.Tenancy;

namespace modular_mlm.Web.Endpoints;

public sealed class OrganizationAdministration : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations";

    public static void Map(RouteGroupBuilder group)
    {
        group
            .MapPost(CreateOrganization, string.Empty)
            .RequireAuthorization(policy => policy.RequireRole(Roles.PlatformAdministrator));
        group.MapGet(GetHostPublicConfig, "public-config").AllowAnonymous();

        group
            .MapGet(GetSettings, "{organizationId:guid}/admin/settings")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPut(UpdateProfile, "{organizationId:guid}/admin/profile")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPut(UpdateCommerceSettings, "{organizationId:guid}/admin/commerce-settings")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPost(PublishBranding, "{organizationId:guid}/admin/branding/publish")
            .RequireAuthorization(policy =>
                policy.RequireRole(Roles.Administrator, Roles.PlatformAdministrator)
            );
        group
            .MapPost(ConfigureDomain, "{organizationId:guid}/admin/domains")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapDelete(
                RemoveDomain,
                "{organizationId:guid}/admin/domains/{organizationDomainId:guid}"
            )
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public static async Task<Created<Guid>> CreateOrganization(
        ISender sender,
        CreateOrganizationRequest request
    )
    {
        var id = await sender.Send(
            new CreateOrganizationCommand(
                request.Name,
                request.Slug,
                request.CurrencyCode,
                request.TimeZone,
                request.Locale
            )
        );
        return TypedResults.Created($"/api/organizations/{id}/admin/settings", id);
    }

    public static Results<Ok<PublicOrganizationConfigDto>, NotFound> GetHostPublicConfig(
        ResolvedOrganizationContext context
    ) =>
        context.Organization is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(context.Organization.PublicConfig);

    public static async Task<Results<Ok<AdminOrganizationSettingsDto>, NotFound>> GetSettings(
        ISender sender,
        Guid organizationId
    )
    {
        var settings = await sender.Send(new GetAdminOrganizationSettingsQuery(organizationId));
        return settings is null ? TypedResults.NotFound() : TypedResults.Ok(settings);
    }

    public static async Task<NoContent> UpdateProfile(
        ISender sender,
        Guid organizationId,
        UpdateOrganizationProfileRequest request
    )
    {
        await sender.Send(
            new UpdateOrganizationProfileCommand(
                organizationId,
                request.Name,
                request.CurrencyCode,
                request.TimeZone,
                request.Locale
            )
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> UpdateCommerceSettings(
        ISender sender,
        Guid organizationId,
        UpdateCommerceSettingsRequest request
    )
    {
        await sender.Send(
            new UpdateCommerceSettingsCommand(
                organizationId,
                request.AllowGuestCheckout,
                request.RequireShippingAddress,
                request.RequireBillingAddress,
                request.InventoryReservationMinutes
            )
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> PublishBranding(ISender sender, Guid organizationId)
    {
        await sender.Send(new PublishBrandingCommand(organizationId));
        return TypedResults.NoContent();
    }

    public static async Task<Created<Guid>> ConfigureDomain(
        ISender sender,
        Guid organizationId,
        ConfigureOrganizationDomainRequest request
    )
    {
        var id = await sender.Send(
            new ConfigureOrganizationDomainCommand(
                organizationId,
                request.HostName,
                request.MakePrimary
            )
        );
        return TypedResults.Created($"/api/organizations/{organizationId}/admin/domains/{id}", id);
    }

    public static async Task<NoContent> RemoveDomain(
        ISender sender,
        Guid organizationId,
        Guid organizationDomainId
    )
    {
        await sender.Send(
            new RemoveOrganizationDomainCommand(organizationId, organizationDomainId)
        );
        return TypedResults.NoContent();
    }
}
