using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Organizations;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Organizations;

public sealed class CommerceSettingsTests
{
    [Test]
    public void DefaultReflectsTheCurrentMarketplaceCheckoutBehavior()
    {
        var settings = CommerceSettings.Default();

        settings.AllowGuestCheckout.ShouldBeTrue();
        settings.RequireShippingAddress.ShouldBeTrue();
        settings.RequireBillingAddress.ShouldBeTrue();
        settings.InventoryReservationMinutes.ShouldBe(30);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(1_441)]
    public void CreateRejectsAnInvalidInventoryReservationPeriod(int minutes)
    {
        var exception = Should.Throw<DomainInvariantException>(() =>
            CommerceSettings.Create(true, true, true, minutes)
        );

        exception.Message.ShouldContain("Inventory reservation");
    }

    [Test]
    public void UpdateAppliesAValidConfiguration()
    {
        var settings = CommerceSettings.Default();

        settings.Update(false, false, true, 60);

        settings.AllowGuestCheckout.ShouldBeFalse();
        settings.RequireShippingAddress.ShouldBeFalse();
        settings.RequireBillingAddress.ShouldBeTrue();
        settings.InventoryReservationMinutes.ShouldBe(60);
    }
}
