using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetOrderDetails.Models;

public sealed record OrderItemDetailsDto(
    Guid Id,
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    decimal BusinessVolume,
    FulfillmentStatus FulfillmentStatus,
    decimal RefundedQuantity,
    decimal RefundableQuantity,
    bool CanRequestRefund,
    string? RefundFailureReason
);

public sealed record AddressDto(
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
