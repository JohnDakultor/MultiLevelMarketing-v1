using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Customers.Commands.RemoveCustomerAddress;

[Authorize]
public sealed record RemoveCustomerAddressCommand(Guid OrganizationId, Guid AddressId)
    : IRequest,
        ICurrentCustomerRequest;
