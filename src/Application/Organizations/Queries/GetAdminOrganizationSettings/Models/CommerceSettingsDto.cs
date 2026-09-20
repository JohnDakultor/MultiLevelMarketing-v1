namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

public sealed record CommerceSettingsDto(
    bool AllowGuestCheckout,
    bool RequireShippingAddress,
    bool RequireBillingAddress,
    int InventoryReservationMinutes
);
