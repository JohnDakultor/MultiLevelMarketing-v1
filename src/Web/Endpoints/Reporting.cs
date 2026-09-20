using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Reporting.Models;
using modular_mlm.Application.Reporting.Queries.GetAdminDashboard;
using modular_mlm.Application.Reporting.Queries.GetAdminReport;
using modular_mlm.Application.Reporting.Queries.GetAgentReport;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class Reporting : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/reports";

    public static void Map(RouteGroupBuilder group)
    {
        group
            .MapGet(GetAdminReport, "admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapGet(GetAdminDashboard, "admin/dashboard")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapGet(GetAgentReport, "agent")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Agent));
    }

    public static async Task<Ok<AdminReportDto>> GetAdminReport(
        ISender sender,
        Guid organizationId,
        DateTimeOffset from,
        DateTimeOffset to
    ) => TypedResults.Ok(await sender.Send(new GetAdminReportQuery(organizationId, from, to)));

    public static async Task<Ok<AdminDashboardDto>> GetAdminDashboard(
        ISender sender,
        Guid organizationId
    ) => TypedResults.Ok(await sender.Send(new GetAdminDashboardQuery(organizationId)));

    public static async Task<Ok<AgentReportDto>> GetAgentReport(
        ISender sender,
        Guid organizationId,
        DateTimeOffset from,
        DateTimeOffset to
    ) => TypedResults.Ok(await sender.Send(new GetAgentReportQuery(organizationId, from, to)));
}
