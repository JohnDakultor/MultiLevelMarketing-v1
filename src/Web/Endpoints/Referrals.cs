using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Organizations.Queries.GetReferralSettings;
using modular_mlm.Application.Referrals.Commands.RegenerateReferralCode;
using modular_mlm.Application.Referrals.Queries.GetAgentStorefront;
using modular_mlm.Application.Referrals.Queries.GetAgentStorefront.Models;
using modular_mlm.Application.Referrals.Queries.GetReferralDashboard;
using modular_mlm.Application.Referrals.Queries.GetReferralDashboard.Models;
using modular_mlm.Application.Referrals.Queries.GetReferralLink;
using modular_mlm.Application.Referrals.Queries.GetReferralLink.Models;
using modular_mlm.Application.Referrals.Queries.ResolveReferralAttribution;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Referral;
using modular_mlm.Web.Models;
using modular_mlm.Web.Services;

namespace modular_mlm.Web.Endpoints;

public sealed class Referrals : IEndpointGroup
{
    public static string? RoutePrefix => "/api/organizations/{organizationId:guid}/referrals";

    public static void Map(RouteGroupBuilder group)
    {
        group
            .MapPost(RegenerateCode, "agents/{agentId:guid}/code")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group
            .MapGet(GetDashboard, "agents/{agentId:guid}/dashboard")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group
            .MapGet(GetLink, "agents/{agentId:guid}/link")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group.MapGet(GetStorefront, "store/{referralCode}").AllowAnonymous();

        group.MapGet(GetResolveReferralAttribution, "resolve/{referralCode}").AllowAnonymous();
    }

    public static async Task<Ok<string>> RegenerateCode(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) =>
        TypedResults.Ok(
            await sender.Send(new RegenerateReferralCodeCommand(organizationId, agentId))
        );

    public static async Task<Ok<ReferralDashboardDto>> GetDashboard(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) => TypedResults.Ok(await sender.Send(new GetReferralDashboardQuery(organizationId, agentId)));

    public static async Task<Ok<ReferralLinkDto>> GetLink(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        Guid? productId
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetReferralLinkQuery(organizationId, agentId, productId))
        );

    public static async Task<Ok<AgentStorefrontDto>> GetStorefront(
        ISender sender,
        ProtectedCookieService cookies,
        HttpRequest request,
        HttpResponse response,
        TimeProvider clock,
        Guid organizationId,
        string referralCode,
        int pageNumber = 1,
        int pageSize = 24
    )
    {
        var storefront = await sender.Send(
            new GetAgentStorefrontQuery(organizationId, referralCode, pageNumber, pageSize)
        );
        var settings = await sender.Send(new GetReferralSettingsQuery(organizationId));

        WriteReferralCookie(
            cookies,
            request,
            response,
            clock,
            organizationId,
            storefront.ReferralCode,
            AttributionSource.Storefront,
            TimeSpan.FromDays(settings.AttributionWindowDays),
            settings.AllowReferralOverride
        );

        return TypedResults.Ok(storefront);
    }

    public static async Task<Results<NoContent, NotFound>> GetResolveReferralAttribution(
        ISender sender,
        ProtectedCookieService cookies,
        HttpRequest request,
        HttpResponse response,
        TimeProvider clock,
        Guid organizationId,
        string referralCode
    )
    {
        var result = await sender.Send(
            new ResolveReferralAttributionQuery(organizationId, referralCode)
        );
        if (result is null)
            return TypedResults.NotFound();
        var settings = await sender.Send(new GetReferralSettingsQuery(organizationId));

        WriteReferralCookie(
            cookies,
            request,
            response,
            clock,
            result.OrganizationId,
            result.ReferralCode,
            AttributionSource.ReferralLink,
            TimeSpan.FromDays(settings.AttributionWindowDays),
            settings.AllowReferralOverride
        );

        return TypedResults.NoContent();
    }

    private static void WriteReferralCookie(
        ProtectedCookieService cookies,
        HttpRequest request,
        HttpResponse response,
        TimeProvider clock,
        Guid organizationId,
        string referralCode,
        AttributionSource source,
        TimeSpan attributionLifetime,
        bool allowOverride
    )
    {
        var cookieName = ReferralAttributionCookie.GetName(organizationId);
        var now = clock.GetUtcNow();
        var existing = cookies.Read<ReferralAttributionCookie>(request, cookieName);
        var existingIsValid =
            existing is not null
            && existing.OrganizationId == organizationId
            && existing.CapturedAt >= now.Subtract(attributionLifetime);

        if (existingIsValid && !allowOverride)
            return;

        cookies.Write(
            response,
            cookieName,
            new ReferralAttributionCookie(organizationId, referralCode, now, source),
            attributionLifetime
        );
    }
}
