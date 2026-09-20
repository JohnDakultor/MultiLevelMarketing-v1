using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile.Models;

namespace modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile;

[Authorize]
public sealed record GetCurrentCustomerProfileQuery(Guid OrganizationId)
    : IRequest<CustomerProfileDto>,
        ICurrentCustomerRequest;
