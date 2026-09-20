using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Organizations;

public sealed class CommerceSettings
{
    public const int MinimumInventoryReservationMinutes = 1;
    public const int MaximumInventoryReservationMinutes = 1_440;

    private CommerceSettings() { }

    public bool AllowGuestCheckout { get; private set; }
    public bool RequireShippingAddress { get; private set; }
    public bool RequireBillingAddress { get; private set; }
    public int InventoryReservationMinutes { get; private set; }

    public static CommerceSettings Default() =>
        Create(
            allowGuestCheckout: true,
            requireShippingAddress: true,
            requireBillingAddress: true,
            inventoryReservationMinutes: 30
        );

    public static CommerceSettings Create(
        bool allowGuestCheckout,
        bool requireShippingAddress,
        bool requireBillingAddress,
        int inventoryReservationMinutes
    )
    {
        var settings = new CommerceSettings();
        settings.Update(
            allowGuestCheckout,
            requireShippingAddress,
            requireBillingAddress,
            inventoryReservationMinutes
        );
        return settings;
    }

    public void Update(
        bool allowGuestCheckout,
        bool requireShippingAddress,
        bool requireBillingAddress,
        int inventoryReservationMinutes
    )
    {
        if (
            inventoryReservationMinutes
            is < MinimumInventoryReservationMinutes
                or > MaximumInventoryReservationMinutes
        )
        {
            throw new DomainInvariantException(
                $"Inventory reservation must be between {MinimumInventoryReservationMinutes} and {MaximumInventoryReservationMinutes} minutes."
            );
        }

        AllowGuestCheckout = allowGuestCheckout;
        RequireShippingAddress = requireShippingAddress;
        RequireBillingAddress = requireBillingAddress;
        InventoryReservationMinutes = inventoryReservationMinutes;
    }
}
