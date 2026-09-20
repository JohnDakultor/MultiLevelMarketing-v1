using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Customers.Commands.UpdateCustomerAddress;

[Authorize]
public sealed record UpdateCustomerAddressCommand(
    Guid OrganizationId,
    Guid AddressId,
    string Label,
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string CityOrMunicipality,
    string Province,
    string PostalCode,
    string CountryCode,
    bool MakeDefault
) : IRequest, ICurrentCustomerRequest;
