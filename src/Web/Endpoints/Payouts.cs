using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Payouts.Commands.CancelPayout;
using modular_mlm.Application.Payouts.Commands.MakeDefaultPayoutAccount;
using modular_mlm.Application.Payouts.Commands.RegisterPayoutAccount;
using modular_mlm.Application.Payouts.Commands.SubmitPayoutAccountForVerification;
using modular_mlm.Application.Payouts.Queries.GetPayoutAccounts;
using modular_mlm.Application.Payouts.Queries.GetPayoutAccounts.Models;
using modular_mlm.Application.Payouts.Queries.GetPayoutDetails;
using modular_mlm.Application.Payouts.Queries.GetPayoutDetails.Models;
using modular_mlm.Application.Payouts.Queries.GetPayoutHistory;
using modular_mlm.Application.Payouts.Queries.GetPayoutHistory.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class Payouts : IEndpointGroup
{
    public static string RoutePrefix =>
        "/api/organizations/{organizationId:guid}/agents/{agentId:guid}/payouts";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Agent, Roles.Administrator));
        group.RequireRateLimiting(RateLimitPolicyNames.Payout);
        group.MapGet(GetAgentPayoutHistory);
        group.MapGet(GetAgentPayoutDetails, "{payoutRequestId:guid}");
        group.MapPost(Cancel, "{payoutRequestId:guid}/cancel");
        group.MapGet(GetAccounts, "accounts");
        group.MapPost(RegisterAccount, "accounts");
        group.MapPost(SubmitAccount, "accounts/{payoutAccountId:guid}/submit");
        group.MapPost(MakeDefault, "accounts/{payoutAccountId:guid}/default");
    }

    public static async Task<Ok<IReadOnlyList<PayoutHistoryItemDto>>> GetAgentPayoutHistory(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        int page = 1,
        int pageSize = 20
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetPayoutHistoryQuery(organizationId, agentId, page, pageSize))
        );

    public static async Task<Results<Ok<PayoutDetailsDto>, NotFound>> GetAgentPayoutDetails(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        Guid payoutRequestId
    )
    {
        var result = await sender.Send(new GetPayoutDetailsQuery(organizationId, payoutRequestId));
        return result is null || result.AgentId != agentId
            ? TypedResults.NotFound()
            : TypedResults.Ok(result);
    }

    public static async Task<Ok<IReadOnlyList<PayoutAccountDto>>> GetAccounts(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) => TypedResults.Ok(await sender.Send(new GetPayoutAccountsQuery(organizationId, agentId)));

    public static async Task<Created<Guid>> RegisterAccount(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        RegisterPayoutAccountRequest request
    )
    {
        var id = await sender.Send(
            new RegisterPayoutAccountCommand(
                organizationId,
                agentId,
                request.Method,
                request.AccountName,
                request.AccountNumber,
                request.BankCode,
                request.Rail
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/agents/{agentId}/payouts/accounts/{id}",
            id
        );
    }

    public static async Task<NoContent> SubmitAccount(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        Guid payoutAccountId
    )
    {
        await sender.Send(
            new SubmitPayoutAccountForVerificationCommand(organizationId, agentId, payoutAccountId)
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> MakeDefault(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        Guid payoutAccountId
    )
    {
        await sender.Send(
            new MakeDefaultPayoutAccountCommand(organizationId, agentId, payoutAccountId)
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> Cancel(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        Guid payoutRequestId
    )
    {
        await sender.Send(new CancelPayoutCommand(organizationId, agentId, payoutRequestId));
        return TypedResults.NoContent();
    }
}

public sealed record RegisterPayoutAccountRequest(
    string Method,
    string AccountName,
    string AccountNumber,
    string BankCode,
    string Rail
);
