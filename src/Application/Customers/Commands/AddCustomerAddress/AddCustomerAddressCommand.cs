using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Customers.Commands.AddCustomerAddress;

[Authorize]
public sealed record AddCustomerAddressCommand(
    Guid OrganizationId,
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
) : IRequest<Guid>, ICurrentCustomerRequest;
