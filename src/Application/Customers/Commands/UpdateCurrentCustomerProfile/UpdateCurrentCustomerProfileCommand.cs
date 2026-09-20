using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile.Models;

namespace modular_mlm.Application.Customers.Commands.UpdateCurrentCustomerProfile;

[Authorize]
public sealed record UpdateCurrentCustomerProfileCommand(Guid OrganizationId, string DisplayName)
    : IRequest<CustomerProfileDto>,
        ICurrentCustomerRequest;
