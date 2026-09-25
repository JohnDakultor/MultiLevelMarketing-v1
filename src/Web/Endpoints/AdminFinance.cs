using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger;
using modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger.Model;
using modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries;
using modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries.Models;
using modular_mlm.Application.Wallets.Queries.GetAdminWallets;
using modular_mlm.Application.Wallets.Queries.GetAdminWallets.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Wallets;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class AdminWallets : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/wallets";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.RequireRateLimiting(RateLimitPolicyNames.Financial);
        group.MapGet(GetWallets);
        group.MapGet(GetEntries, "{agentId:guid}/entries");
    }

    public static async Task<Ok<AdminWalletsPageDto>> GetWallets(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        WalletStatus? status = null,
        bool negativeOnly = false
    ) => TypedResults.Ok(await sender.Send(
        new GetAdminWalletsQuery(organizationId, page, pageSize, search, status, negativeOnly)
    ));

    public static async Task<Ok<AdminWalletEntriesPageDto>> GetEntries(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        int page = 1,
        int pageSize = 20,
        WalletEntryType? entryType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null
    ) => TypedResults.Ok(await sender.Send(
        new GetAdminWalletEntriesQuery(organizationId, agentId, page, pageSize, entryType, from, to)
    ));
}

public sealed class AdminCommissions : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/commissions";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.RequireRateLimiting(RateLimitPolicyNames.Financial);
        group.MapGet(GetLedger);
    }

    public static async Task<Ok<AdminCommissionLedgerPageDto>> GetLedger(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        Guid? agentId = null,
        string? commissionType = null,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        bool includeReversals = false
    ) => TypedResults.Ok(await sender.Send(new GetAdminCommissionLedgerQuery(
        organizationId,
        page,
        pageSize,
        agentId,
        commissionType,
        status,
        from,
        to,
        includeReversals
    )));
}
