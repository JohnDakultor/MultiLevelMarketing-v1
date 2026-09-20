using System.Text.RegularExpressions;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Identity;

public sealed class CustomerAddress : OrganizationEntity
{
    public const int MaximumLabelLength = 50;
    public const int MaximumRecipientNameLength = 200;
    public const int MaximumPhoneNumberLength = 32;
    public const int MaximumAddressLineLength = 255;
    public const int MaximumBarangayLength = 100;
    public const int MaximumLocalityLength = 100;
    public const int MaximumPostalCodeLength = 20;

    private static readonly Regex IsoCountryRegex = new(@"^[A-Z]{2,3}$", RegexOptions.Compiled);

    private CustomerAddress() { }

    public Guid CustomerProfileId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string? Barangay { get; private set; } // Kept optional
    public string CityOrMunicipality { get; private set; } = string.Empty;
    public string Province { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static CustomerAddress Create(
        Guid organizationId,
        Guid customerProfileId,
        string label,
        string recipientName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string? barangay,
        string cityOrMunicipality,
        string province,
        string postalCode,
        string countryCode
    )
    {
        if (organizationId == Guid.Empty || customerProfileId == Guid.Empty)
            throw new DomainInvariantException("Organization and customer ownership are required.");

        ValidateBusinessRules(
            label,
            recipientName,
            phoneNumber,
            addressLine1,
            addressLine2,
            barangay,
            cityOrMunicipality,
            province,
            postalCode,
            countryCode
        );

        return new CustomerAddress
        {
            OrganizationId = organizationId,
            CustomerProfileId = customerProfileId,
            Label = label.Trim(),
            RecipientName = recipientName.Trim(),
            PhoneNumber = NormalizePhoneNumber(phoneNumber),
            AddressLine1 = addressLine1.Trim(),
            AddressLine2 = addressLine2?.Trim(),
            Barangay = barangay?.Trim(), // Safely handle null without failing validation
            CityOrMunicipality = cityOrMunicipality.Trim(),
            Province = province.Trim(),
            PostalCode = NormalizePostalCode(postalCode),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            IsActive = true,
        };
    }

    public void UpdateDeliveryDetails(
        string label,
        string recipientName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string? barangay,
        string cityOrMunicipality,
        string province,
        string postalCode,
        string countryCode
    )
    {
        if (!IsActive)
            throw new DomainInvariantException("Cannot update an archived address.");

        ValidateBusinessRules(
            label,
            recipientName,
            phoneNumber,
            addressLine1,
            addressLine2,
            barangay,
            cityOrMunicipality,
            province,
            postalCode,
            countryCode
        );

        Label = label.Trim();
        RecipientName = recipientName.Trim();
        PhoneNumber = NormalizePhoneNumber(phoneNumber);
        AddressLine1 = addressLine1.Trim();
        AddressLine2 = addressLine2?.Trim();
        Barangay = barangay?.Trim();
        CityOrMunicipality = cityOrMunicipality.Trim();
        Province = province.Trim();
        PostalCode = NormalizePostalCode(postalCode);
        CountryCode = countryCode.Trim().ToUpperInvariant();
    }

    public void Archive() => IsActive = false;

    private static void ValidateBusinessRules(
        string label,
        string recipientName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string? barangay,
        string cityOrMunicipality,
        string province,
        string postalCode,
        string countryCode
    )
    {
        if (
            string.IsNullOrWhiteSpace(label)
            || string.IsNullOrWhiteSpace(recipientName)
            || string.IsNullOrWhiteSpace(phoneNumber)
            || string.IsNullOrWhiteSpace(addressLine1)
            || string.IsNullOrWhiteSpace(cityOrMunicipality)
            || string.IsNullOrWhiteSpace(province)
            || string.IsNullOrWhiteSpace(postalCode)
            || string.IsNullOrWhiteSpace(countryCode)
        )
        {
            throw new DomainInvariantException("All required delivery fields must be provided.");
        }

        ValidateLength(label, MaximumLabelLength, "Address label");
        ValidateLength(recipientName, MaximumRecipientNameLength, "Recipient name");
        ValidateLength(phoneNumber, MaximumPhoneNumberLength, "Phone number");
        ValidateLength(addressLine1, MaximumAddressLineLength, "Address line 1");
        ValidateOptionalLength(addressLine2, MaximumAddressLineLength, "Address line 2");
        ValidateOptionalLength(barangay, MaximumBarangayLength, "Barangay");
        ValidateLength(cityOrMunicipality, MaximumLocalityLength, "City or municipality");
        ValidateLength(province, MaximumLocalityLength, "Province");
        ValidateLength(postalCode, MaximumPostalCodeLength, "Postal code");

        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
        if (!IsoCountryRegex.IsMatch(normalizedCountry))
        {
            throw new DomainInvariantException(
                "Country code must be a valid ISO standard (2 or 3 alphabetic characters)."
            );
        }
    }

    private static void ValidateLength(string value, int maximumLength, string fieldName)
    {
        if (value.Trim().Length > maximumLength)
            throw new DomainInvariantException(
                $"{fieldName} cannot exceed {maximumLength} characters."
            );
    }

    private static void ValidateOptionalLength(string? value, int maximumLength, string fieldName)
    {
        if (value?.Trim().Length > maximumLength)
            throw new DomainInvariantException(
                $"{fieldName} cannot exceed {maximumLength} characters."
            );
    }

    private static string NormalizePhoneNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var digitsOnly = new string(input.Where(char.IsDigit).ToArray());
        return input.Trim().StartsWith('+') ? $"+{digitsOnly}" : digitsOnly;
    }

    private static string NormalizePostalCode(string input)
    {
        return input.Replace(" ", "").Replace("-", "").Trim().ToUpperInvariant();
    }
}
