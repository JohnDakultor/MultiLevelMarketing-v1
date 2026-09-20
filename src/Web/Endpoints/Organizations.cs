using Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Organizations.Commands.UpdateBranding;
using modular_mlm.Application.Organizations.Commands.UpdateFeatureSettings;
using modular_mlm.Application.Organizations.Commands.UpdateNetworkSettings;
using modular_mlm.Application.Organizations.Commands.UpdateWalletSettings;
using modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig;
using modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig.Models;
using modular_mlm.Application.Organizations.Queries.GetWalletSettings;
using modular_mlm.Application.Organizations.Queries.GetWalletSettings.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Network;

namespace modular_mlm.Web.Endpoints;

public sealed class Organizations : IEndpointGroup
{
    public static string? RoutePrefix => "/api/organizations";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetPublicConfig, "{slug}/public-config").AllowAnonymous();
        group
            .MapPut(UpdateNetworkSettings, "{organizationId:guid}/network-settings")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapGet(GetWalletSettings, "{organizationId:guid}/wallet-settings")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPut(UpdateWalletSettings, "{organizationId:guid}/wallet-settings")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPut(UpdateBranding, "{organizationId:guid}/branding")
            .RequireAuthorization(policy =>
                policy.RequireRole(Roles.Administrator, Roles.PlatformAdministrator)
            );
        group
            .MapPut(UpdateFeatureSettings, "{organizationId:guid}/feature-settings")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public static async Task<Results<Ok<PublicOrganizationConfigDto>, NotFound>> GetPublicConfig(
        ISender sender,
        string slug
    )
    {
        var result = await sender.Send(new GetPublicOrganizationConfigQuery(slug));
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    public static async Task<NoContent> UpdateNetworkSettings(
        ISender sender,
        Guid organizationId,
        UpdateNetworkSettingsRequest request
    )
    {
        await sender.Send(
            new UpdateNetworkSettingsCommand(
                organizationId,
                request.DefaultPlacementStrategy,
                request.AllowAgentPreferredLeg,
                request.MaxQueryDepth,
                request.AutoPlacementEnabled,
                request.RestrictPlacementChangesAfterActivation
            )
        );
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<WalletSettingsDto>, NotFound>> GetWalletSettings(
        ISender sender,
        Guid organizationId
    )
    {
        var result = await sender.Send(new GetWalletSettingsQuery(organizationId));
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    public static async Task<NoContent> UpdateWalletSettings(
        ISender sender,
        Guid organizationId,
        UpdateWalletSettingsRequest request
    )
    {
        await sender.Send(
            new UpdateWalletSettingsCommand(
                organizationId,
                request.CommissionReleaseTrigger,
                request.ReleaseDelayDays,
                request.ReturnWindowDays,
                request.MinimumPayoutAmount,
                request.AllowNegativeRecoverableBalance,
                request.MaximumNegativeBalance
            )
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> UpdateBranding(
        ISender sender,
        Guid organizationId,
        UpdateBrandingRequest request
    )
    {
        await sender.Send(
            new UpdateBrandingCommand(
                organizationId,
                request.StoreTitle,
                request.SupportEmail,
                request.PrimaryColor,
                request.SecondaryColor,
                request.AccentColor,
                request.LogoUrl,
                request.FaviconUrl,
                request.SupportPhone,
                request.FooterText
            )
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> UpdateFeatureSettings(
        ISender sender,
        Guid organizationId,
        UpdateFeatureSettingsRequest request
    )
    {
        await sender.Send(
            new UpdateFeatureSettingsCommand(
                organizationId,
                request.CommerceEnabled,
                request.AgentProgramEnabled,
                request.BinaryNetworkEnabled,
                request.BinaryPairingEnabled,
                request.WalletEnabled,
                request.PayoutEnabled
            )
        );
        return TypedResults.NoContent();
    }
}

public sealed record UpdateNetworkSettingsRequest(
    PlacementStrategyType DefaultPlacementStrategy,
    bool AllowAgentPreferredLeg,
    int MaxQueryDepth,
    bool AutoPlacementEnabled,
    bool RestrictPlacementChangesAfterActivation
);

public sealed record UpdateWalletSettingsRequest(
    CommissionReleaseTrigger CommissionReleaseTrigger,
    int ReleaseDelayDays,
    int ReturnWindowDays,
    decimal MinimumPayoutAmount,
    bool AllowNegativeRecoverableBalance,
    decimal MaximumNegativeBalance
);

public sealed record UpdateBrandingRequest(
    string StoreTitle,
    string SupportEmail,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string? LogoUrl,
    string? FaviconUrl,
    string? SupportPhone,
    string? FooterText
);

public sealed record UpdateFeatureSettingsRequest(
    bool CommerceEnabled,
    bool AgentProgramEnabled,
    bool BinaryNetworkEnabled,
    bool BinaryPairingEnabled,
    bool WalletEnabled,
    bool PayoutEnabled
);
