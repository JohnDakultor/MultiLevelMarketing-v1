using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Payouts.Commands.ApprovePayout;
using modular_mlm.Application.Payouts.Commands.ProcessPayout;
using modular_mlm.Application.Payouts.Commands.ReconcilePayout;
using modular_mlm.Application.Payouts.Commands.RejectPayout;
using modular_mlm.Application.Payouts.Commands.RejectPayoutAccount;
using modular_mlm.Application.Payouts.Commands.VerifyPayoutAccount;
using modular_mlm.Application.Payouts.Queries.GetPayoutDetails;
using modular_mlm.Application.Payouts.Queries.GetPayoutDetails.Models;
using modular_mlm.Application.Payouts.Queries.GetPayoutHistory;
using modular_mlm.Application.Payouts.Queries.GetPayoutHistory.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class PayoutAdministration : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/payouts";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.RequireRateLimiting(RateLimitPolicyNames.AdministratorFinancialAction);
        group.MapGet(GetAdministratorPayoutHistory);
        group.MapGet(GetAdministratorPayoutDetails, "{payoutRequestId:guid}");
        group.MapPost(Approve, "{payoutRequestId:guid}/approve");
        group.MapPost(Reject, "{payoutRequestId:guid}/reject");
        group.MapPost(ProcessPayout, "{payoutRequestId:guid}/process");
        group.MapPost(Reconcile, "{payoutRequestId:guid}/reconcile");
        group.MapPost(VerifyAccount, "accounts/{payoutAccountId:guid}/verify");
        group.MapPost(RejectAccount, "accounts/{payoutAccountId:guid}/reject");
    }

    public static async Task<Ok<IReadOnlyList<PayoutHistoryItemDto>>> GetAdministratorPayoutHistory(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetPayoutHistoryQuery(organizationId, null, page, pageSize))
        );

    public static async Task<Results<Ok<PayoutDetailsDto>, NotFound>> GetAdministratorPayoutDetails(
        ISender sender,
        Guid organizationId,
        Guid payoutRequestId
    )
    {
        var result = await sender.Send(new GetPayoutDetailsQuery(organizationId, payoutRequestId));
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    public static async Task<NoContent> Approve(
        ISender sender,
        Guid organizationId,
        Guid payoutRequestId
    )
    {
        await sender.Send(new ApprovePayoutCommand(organizationId, payoutRequestId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> Reject(
        ISender sender,
        Guid organizationId,
        Guid payoutRequestId
    )
    {
        await sender.Send(new RejectPayoutCommand(organizationId, payoutRequestId));
        return TypedResults.NoContent();
    }

    public static async Task<Ok<string>> ProcessPayout(
        ISender sender,
        Guid organizationId,
        Guid payoutRequestId
    ) =>
        TypedResults.Ok(
            await sender.Send(new ProcessPayoutCommand(organizationId, payoutRequestId))
        );

    public static async Task<Ok<bool>> Reconcile(
        ISender sender,
        Guid organizationId,
        Guid payoutRequestId
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new ReconcilePayoutForOrganizationCommand(organizationId, payoutRequestId)
            )
        );

    public static async Task<NoContent> VerifyAccount(
        ISender sender,
        Guid organizationId,
        Guid payoutAccountId
    )
    {
        await sender.Send(new VerifyPayoutAccountCommand(organizationId, payoutAccountId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> RejectAccount(
        ISender sender,
        Guid organizationId,
        Guid payoutAccountId
    )
    {
        await sender.Send(new RejectPayoutAccountCommand(organizationId, payoutAccountId));
        return TypedResults.NoContent();
    }
}
