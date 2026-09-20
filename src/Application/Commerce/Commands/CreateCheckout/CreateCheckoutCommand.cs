namespace modular_mlm.Application.Commerce.Commands.CreateCheckout;

public sealed record CheckoutAddressInput(
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string CityOrMunicipality,
    string Province,
    string PostalCode,
    string CountryCode
);

public sealed record CreateCheckoutCommand(
    Guid OrganizationId,
    CheckoutAddressInput ShippingAddress,
    CheckoutAddressInput BillingAddress
) : IRequest<Guid>, ICurrentCustomerRequest;
