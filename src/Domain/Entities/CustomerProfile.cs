using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Identity;

public sealed class CustomerProfile : OrganizationEntity
{
    public const int MaximumDisplayNameLength = 200;
    public const int MaximumEmailLength = 320;

    private readonly List<CustomerAddress> _addresses = [];

    private CustomerProfile() { }

    public string UserId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public CustomerStatus Status { get; private set; }
    public Guid? DefaultAddressId { get; private set; }

    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

    public static CustomerProfile Create(
        Guid organizationId,
        string userId,
        string displayName,
        string? email = null
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(userId))
            throw new DomainInvariantException("Organization and user are required.");
        ValidateDisplayName(displayName);
        var normalizedEmail = NormalizeEmail(email);

        return new CustomerProfile
        {
            OrganizationId = organizationId,
            UserId = userId,
            DisplayName = displayName.Trim(),
            Email = normalizedEmail,
            Status = CustomerStatus.Active,
        };
    }

    public void Suspend()
    {
        if (Status != CustomerStatus.Active)
            throw new DomainInvariantException("Only an active customer can be suspended.");
        Status = CustomerStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status != CustomerStatus.Suspended)
            throw new DomainInvariantException("Only a suspended customer can be reactivated.");
        Status = CustomerStatus.Active;
    }

    public void Disable()
    {
        if (Status == CustomerStatus.Disabled)
            throw new DomainInvariantException("The customer is already disabled.");
        Status = CustomerStatus.Disabled;
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

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;
        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > MaximumEmailLength || !normalized.Contains('@'))
            throw new DomainInvariantException("A valid customer email is required.");
        return normalized;
    }
}
