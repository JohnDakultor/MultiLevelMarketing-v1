using modular_mlm.Domain.Referral;

namespace modular_mlm.Application.Commerce.Commands.CreateCheckout.Models;

internal sealed record CheckoutCartSnapshot(
    Guid CartId,
    Guid OrganizationId,
    Guid CustomerId,
    ReferralAttributionContext? Attribution,
    string CurrencyCode,
    CheckoutAddressSnapshot ShippingAddress,
    CheckoutAddressSnapshot BillingAddress,
    IReadOnlyList<CheckoutCartItemSnapshot> Items
);

internal sealed record CheckoutCartItemSnapshot(
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal CommissionableAmount,
    decimal? DirectSalesRateOverride,
    decimal BusinessVolume,
    Guid? CommissionProfileId
);

internal sealed record CheckoutAddressSnapshot(
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
