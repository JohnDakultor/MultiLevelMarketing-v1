namespace modular_mlm.Application.Customers.Queries.GetCustomerAddresses.Models;

public sealed record CustomerAddressDto(
    Guid Id,
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
    bool IsDefault
);
