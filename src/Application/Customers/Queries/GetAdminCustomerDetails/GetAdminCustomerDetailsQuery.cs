using modular_mlm.Application.Customers.Queries.GetAdminCustomerDetails.Models;

namespace modular_mlm.Application.Customers.Queries.GetAdminCustomerDetails;

public sealed record GetAdminCustomerDetailsQuery(Guid OrganizationId, Guid CustomerId)
    : IRequest<AdminCustomerDetailsDto?>,
        IOrganizationAdminRequest;
