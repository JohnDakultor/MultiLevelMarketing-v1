using Application.Wallets.Queries.GetWalletEntries.Models;
using Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Wallets.Commands.CreateWalletAdjustment;
using modular_mlm.Application.Wallets.Queries.GetWalletEntries;
using modular_mlm.Application.Wallets.Queries.GetWalletSummary;
using modular_mlm.Application.Wallets.Queries.GetWalletSummary.Models;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Web.Endpoints;

public class Wallets : IEndpointGroup
{
    public static string RoutePrefix =>
        "/api/organizations/{organizationId:guid}/agents/{agentId:guid}/wallet";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(policy => policy.RequireRole("Agent", "Administrator"));
        groupBuilder.MapGet("", GetWalletSummary);
        groupBuilder.MapGet("entries", GetWalletEntries);
        groupBuilder
            .MapPost("adjustments", CreateWalletAdjustment)
            .RequireAuthorization(policy => policy.RequireRole("Administrator"))
            .RequireRateLimiting("financial");
    }

    public static async Task<Ok<WalletSummaryDto>> GetWalletSummary(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) => TypedResults.Ok(await sender.Send(new GetWalletSummaryQuery(organizationId, agentId)));

    public static async Task<Ok<WalletEntriesPageDto>> GetWalletEntries(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        int page,
        int pageSize,
        WalletEntryType? entryType,
        DateTimeOffset? from,
        DateTimeOffset? to
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetWalletEntriesQuery(
                    organizationId,
                    agentId,
                    page,
                    pageSize,
                    entryType,
                    from,
                    to
                )
            )
        );

    public static async Task<Created<Guid>> CreateWalletAdjustment(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        CreateWalletAdjustmentRequest request
    )
    {
        var command = new CreateWalletAdjustmentCommand(
            organizationId,
            agentId,
            request.Direction,
            request.Amount,
            request.Currency,
            request.Reason,
            request.IdempotencyKey
        );
        var entryId = await sender.Send(command);
        return TypedResults.Created($"{RoutePrefix}/entries/{entryId}", entryId);
    }

    public sealed record CreateWalletAdjustmentRequest(
        Guid OrganizationId,
        Guid AgentId,
        WalletAdjustmentDirection Direction,
        decimal Amount,
        string Currency,
        string Reason,
        string IdempotencyKey
    );
}
