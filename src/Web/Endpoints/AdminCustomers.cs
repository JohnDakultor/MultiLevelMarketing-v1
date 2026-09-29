using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Customers.Commands.ChangeCustomerStatus;
using modular_mlm.Application.Customers.Queries.GetAdminCustomerDetails;
using modular_mlm.Application.Customers.Queries.GetAdminCustomerDetails.Models;
using modular_mlm.Application.Customers.Queries.GetAdminCustomers;
using modular_mlm.Application.Customers.Queries.GetAdminCustomers.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Identity;

namespace modular_mlm.Web.Endpoints;

public sealed class AdminCustomers : IEndpointGroup
{
    public static string? RoutePrefix => "/api/organizations/{organizationId:guid}/admin/customers";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(List, string.Empty);
        group.MapGet(Details, "{customerId:guid}");
        group.MapPut(ChangeStatus, "{customerId:guid}/status");
    }

    public static async Task<Ok<PagedResponse<AdminCustomerSummaryDto>>> List(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CustomerStatus? status = null,
        CustomerSort sort = CustomerSort.Newest
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAdminCustomersQuery(
                    organizationId,
                    page,
                    pageSize,
                    search,
                    status,
                    sort
                )
            )
        );

    public static async Task<Results<Ok<AdminCustomerDetailsDto>, NotFound>> Details(
        ISender sender,
        Guid organizationId,
        Guid customerId
    )
    {
        var result = await sender.Send(
            new GetAdminCustomerDetailsQuery(organizationId, customerId)
        );
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    public static async Task<NoContent> ChangeStatus(
        ISender sender,
        Guid organizationId,
        Guid customerId,
        ChangeCustomerStatusRequest request
    )
    {
        await sender.Send(
            new ChangeCustomerStatusCommand(
                organizationId,
                customerId,
                request.Status,
                request.Reason
            )
        );
        return TypedResults.NoContent();
    }
}

public sealed record ChangeCustomerStatusRequest(CustomerStatus Status, string Reason);
