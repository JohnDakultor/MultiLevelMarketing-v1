using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Customers.Queries.GetCustomerAddresses.Models;

namespace modular_mlm.Application.Customers.Queries.GetCustomerAddresses;

[Authorize]
public sealed record GetCustomerAddressesQuery(Guid OrganizationId)
    : IRequest<IReadOnlyList<CustomerAddressDto>>,
        ICurrentCustomerRequest;
