namespace modular_mlm.Application.Organizations.Commands.UpdateCommerceSettings;

public sealed record UpdateCommerceSettingsCommand(
    Guid OrganizationId,
    bool AllowGuestCheckout,
    bool RequireShippingAddress,
    bool RequireBillingAddress,
    int InventoryReservationMinutes
) : IRequest, IOrganizationAdminRequest;
