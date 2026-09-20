using modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails.Models;

namespace modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails;

public sealed record GetAdminOrderDetailsQuery(Guid OrganizationId, Guid OrderId)
    : IRequest<AdminOrderDetailsDto?>,
        IOrganizationAdminRequest;
