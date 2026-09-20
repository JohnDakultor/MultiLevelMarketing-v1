using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory;
using modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory.Models;
using modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary;
using modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary.Models;
using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary;
using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary.Models;
using modular_mlm.Application.Compensation.Queries.GetCommissionDetails;
using modular_mlm.Application.Payouts.Commands.RequestPayout;
using modular_mlm.Application.Wallets.Queries.GetWalletSummary;
using modular_mlm.Application.Wallets.Queries.GetWalletSummary.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class Finance : IEndpointGroup
{
    public static string? RoutePrefix =>
        "/api/organizations/{organizationId:guid}/agents/{agentId:guid}/finance";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group.MapGet(GetWallet, "wallet");
        group.MapGet(GetEarnings, "earnings");
        group.MapGet(GetCommissions, "commissions");
        group.MapGet(GetCommission, "commissions/{commissionId:guid}");
        group.MapGet(GetBinaryVolume, "binary-volume");
        group.MapPost(RequestPayout, "payouts").RequireRateLimiting(RateLimitPolicyNames.Payout);
    }

    public static async Task<Results<Ok<WalletSummaryDto>, NotFound>> GetWallet(
        ISender sender,
        Guid organizationId,
        Guid agentId
    )
    {
        var result = await sender.Send(new GetWalletSummaryQuery(organizationId, agentId));
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    public static async Task<Created<Guid>> RequestPayout(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        RequestPayoutRequest request
    )
    {
        var id = await sender.Send(
            new RequestPayoutCommand(
                organizationId,
                agentId,
                request.PayoutAccountId,
                request.Amount,
                request.Currency
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/agents/{agentId}/finance/payouts/{id}",
            id
        );
    }

    public static async Task<Ok<AgentEarningsSummaryDto>> GetEarnings(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetAgentEarningsSummaryQuery(organizationId, agentId))
        );

    public static async Task<Ok<IReadOnlyList<CommissionHistoryItemDto>>> GetCommissions(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        int page = 1,
        int pageSize = 20
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAgentCommissionHistoryQuery(organizationId, agentId, page, pageSize)
            )
        );

    public static async Task<Results<Ok<CommissionHistoryItemDto>, NotFound>> GetCommission(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        Guid commissionId
    )
    {
        var result = await sender.Send(
            new GetCommissionDetailsQuery(organizationId, agentId, commissionId)
        );
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    public static async Task<Ok<BinaryVolumeSummaryDto>> GetBinaryVolume(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetBinaryVolumeSummaryQuery(organizationId, agentId))
        );
}

public sealed record RequestPayoutRequest(Guid PayoutAccountId, decimal Amount, string Currency);
