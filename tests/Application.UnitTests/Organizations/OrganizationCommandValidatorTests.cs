using modular_mlm.Application.Organizations.Commands.CreateOrganization;
using modular_mlm.Application.Organizations.Commands.UpdateCommerceSettings;
using modular_mlm.Application.Organizations.Commands.UpdateOrganizationProfile;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Organizations;

public sealed class OrganizationCommandValidatorTests
{
    private readonly CreateOrganizationCommandValidator _createValidator = new();
    private readonly UpdateOrganizationProfileCommandValidator _profileValidator = new();
    private readonly UpdateCommerceSettingsCommandValidator _commerceValidator = new();

    [Test]
    public void CreateAcceptsAValidOrganizationContract()
    {
        var result = _createValidator.Validate(
            new CreateOrganizationCommand("Green Zero", "green-zero", "PHP", "Asia/Manila", "en-PH")
        );

        result.IsValid.ShouldBeTrue();
    }

    [TestCase("admin")]
    [TestCase("api")]
    [TestCase("Green-Zero")]
    [TestCase("green--zero")]
    [TestCase("-green-zero")]
    public void CreateRejectsReservedOrUnsafeSlugs(string slug)
    {
        var result = _createValidator.Validate(
            new CreateOrganizationCommand("Green Zero", slug, "PHP")
        );

        result.Errors.ShouldContain(error => error.PropertyName == "Slug");
    }

    [Test]
    public void CreateRejectsUnknownLocaleTimeZoneAndInvalidCurrency()
    {
        var result = _createValidator.Validate(
            new CreateOrganizationCommand(
                "Green Zero",
                "green-zero",
                "P1",
                "Not/A-Time-Zone",
                "not_a_locale"
            )
        );

        result.Errors.ShouldContain(error => error.PropertyName == "CurrencyCode");
        result.Errors.ShouldContain(error => error.PropertyName == "TimeZone");
        result.Errors.ShouldContain(error => error.PropertyName == "Locale");
    }

    [Test]
    public void ProfileRequiresTenantAndValidRegionalSettings()
    {
        var result = _profileValidator.Validate(
            new UpdateOrganizationProfileCommand(
                Guid.Empty,
                "",
                "PH",
                "Not/A-Time-Zone",
                "not_a_locale"
            )
        );

        result.Errors.ShouldContain(error => error.PropertyName == "OrganizationId");
        result.Errors.ShouldContain(error => error.PropertyName == "Name");
        result.Errors.ShouldContain(error => error.PropertyName == "CurrencyCode");
        result.Errors.ShouldContain(error => error.PropertyName == "TimeZone");
        result.Errors.ShouldContain(error => error.PropertyName == "Locale");
    }

    [TestCase(0)]
    [TestCase(1_441)]
    public void CommerceRejectsInvalidReservationPeriods(int minutes)
    {
        var result = _commerceValidator.Validate(
            new UpdateCommerceSettingsCommand(Guid.NewGuid(), true, true, true, minutes)
        );

        result.Errors.ShouldContain(error => error.PropertyName == "InventoryReservationMinutes");
    }
}
