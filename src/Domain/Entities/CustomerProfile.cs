using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Identity;

public sealed class CustomerProfile : OrganizationEntity
{
    public const int MaximumDisplayNameLength = 200;

    private readonly List<CustomerAddress> _addresses = [];

    private CustomerProfile() { }

    public string UserId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public Guid? DefaultAddressId { get; private set; }

    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

    public static CustomerProfile Create(Guid organizationId, string userId, string displayName)
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(userId))
            throw new DomainInvariantException("Organization and user are required.");
        ValidateDisplayName(displayName);

        return new CustomerProfile
        {
            OrganizationId = organizationId,
            UserId = userId,
            DisplayName = displayName.Trim(),
        };
    }

    public void Rename(string displayName)
    {
        ValidateDisplayName(displayName);
        DisplayName = displayName.Trim();
    }

    public void SetDefaultAddress(Guid addressId) =>
        DefaultAddressId =
            addressId == Guid.Empty
                ? throw new DomainInvariantException("Address is required.")
                : addressId;

    public void ClearDefaultAddress() => DefaultAddressId = null;

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainInvariantException("Display name is required.");
        if (displayName.Trim().Length > MaximumDisplayNameLength)
            throw new DomainInvariantException(
                $"Display name cannot exceed {MaximumDisplayNameLength} characters."
            );
    }
}
