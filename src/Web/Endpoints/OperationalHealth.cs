using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Monitoring.Queries.GetOperationalHealth;
using modular_mlm.Application.Monitoring.Queries.GetOperationalHealth.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class OperationalHealth : IEndpointGroup
{
    public static string RoutePrefix =>
        "/api/organizations/{organizationId:guid}/admin/operational-health";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetOperationalHealth);
    }

    public static async Task<Ok<OperationalHealthDto>> GetOperationalHealth(
        ISender sender,
        Guid organizationId
    ) => TypedResults.Ok(await sender.Send(new GetOperationalHealthQuery(organizationId)));
}
