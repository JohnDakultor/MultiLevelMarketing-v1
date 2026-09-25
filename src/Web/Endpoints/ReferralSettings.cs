using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Organizations.Commands.UpdateReferralSettings;
using modular_mlm.Application.Organizations.Queries.GetReferralSettings;
using modular_mlm.Application.Organizations.Queries.GetReferralSettings.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class ReferralSettings : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/referral-settings";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetSettings);
        group.MapPut(UpdateSettings, "");
    }

    public static async Task<Ok<ReferralSettingsDto>> GetSettings(ISender sender, Guid organizationId) =>
        TypedResults.Ok(await sender.Send(new GetReferralSettingsQuery(organizationId)));

    public static async Task<NoContent> UpdateSettings(
        ISender sender,
        Guid organizationId,
        UpdateReferralSettingsRequest request
    )
    {
        await sender.Send(new UpdateReferralSettingsCommand(
            organizationId,
            request.AttributionWindowDays,
            request.AllowReferralOverride,
            request.ReferralLockAfterFirstPurchase
        ));
        return TypedResults.NoContent();
    }
}

public sealed record UpdateReferralSettingsRequest(
    int AttributionWindowDays,
    bool AllowReferralOverride,
    bool ReferralLockAfterFirstPurchase
);
