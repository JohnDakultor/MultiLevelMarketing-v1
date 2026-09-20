using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Compensation.Commands.ProcessBinaryPairing;
using modular_mlm.Application.Compensation.Queries.GetPairingHistory;
using modular_mlm.Application.Compensation.Queries.GetPairingHistory.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class Pairing : IEndpointGroup
{
    public static string? RoutePrefix =>
        "/api/organizations/{organizationId:guid}/agents/{agentId:guid}/pairing";

    public static void Map(RouteGroupBuilder group)
    {
        group
            .MapGet(GetPairingHistory)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group
            .MapPost(ProcessPairing, "runs")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public static async Task<Ok<List<BinaryPairingRunDto>>> GetPairingHistory(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetPairingHistoryQuery(organizationId, agentId, periodStart, periodEnd)
            )
        );

    public static async Task<Created<Guid>> ProcessPairing(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        ProcessBinaryPairingRequest request
    )
    {
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"binary:{organizationId:N}:{request.CommissionPlanId:N}:{agentId:N}:{request.PeriodStart.UtcTicks}:{request.PeriodEnd.UtcTicks}"
            : request.IdempotencyKey;
        var runId = await sender.Send(
            new ProcessBinaryPairingForOrganizationCommand(
                organizationId,
                agentId,
                request.CommissionPlanId,
                request.PeriodStart,
                request.PeriodEnd,
                key
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/agents/{agentId}/pairing/runs/{runId}",
            runId
        );
    }
}

public sealed record ProcessBinaryPairingRequest(
    Guid CommissionPlanId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    string? IdempotencyKey
);
