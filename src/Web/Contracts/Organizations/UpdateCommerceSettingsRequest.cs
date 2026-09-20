namespace modular_mlm.Web.Contracts.Organizations;

public sealed record UpdateCommerceSettingsRequest(
    bool AllowGuestCheckout,
    bool RequireShippingAddress,
    bool RequireBillingAddress,
    int InventoryReservationMinutes
);
