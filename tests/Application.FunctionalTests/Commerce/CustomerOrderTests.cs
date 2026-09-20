using modular_mlm.Application.Commerce.Queries.GetOrderDetails;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class CustomerOrderTests : TestBase
{
    private const string AddressJson = """
        {
          "recipientName": "Juan Dela Cruz",
          "phoneNumber": "+639171234567",
          "addressLine1": "123 Test Street",
          "addressLine2": null,
          "barangay": "Test Barangay",
          "cityOrMunicipality": "Manila",
          "province": "Metro Manila",
          "postalCode": "1000",
          "countryCode": "PH"
        }
        """;

    [Test]
    public async Task OwnerReceivesOrderAndImmutableItemSnapshots()
    {
        var data = await CreateOrderAsync("owner");

        var result = await SendAsync(new GetOrderDetailsQuery(data.Organization.Id, data.Order.Id));

        result.Id.ShouldBe(data.Order.Id);
        result.OrderNumber.ShouldBe(data.Order.OrderNumber);
        result.Currency.ShouldBe("PHP");
        result.Subtotal.ShouldBe(250m);
        result.GrandTotal.ShouldBe(250m);
        result.ShippingAddress.CountryCode.ShouldBe("PH");
        result.CanRequestCancellation.ShouldBeTrue();
        result.CancellationFailureReason.ShouldBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].ProductName.ShouldBe("Snapshot Product");
        result.Items[0].Sku.ShouldBe("SNAPSHOT-SKU");
        result.Items[0].Quantity.ShouldBe(2);
        result.Items[0].LineTotal.ShouldBe(250m);
        result.Items[0].CanRequestRefund.ShouldBeFalse();
    }

    [Test]
    public async Task AnotherCustomerCannotReadTheOrderByGuessingItsId()
    {
        var data = await CreateOrderAsync("foreign");
        var intruderUserId = await RunAsUserAsync(
            $"intruder-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        await AddAsync(
            CustomerProfile.Create(data.Organization.Id, intruderUserId, "Other Customer")
        );

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetOrderDetailsQuery(data.Organization.Id, data.Order.Id))
        );
    }

    private static async Task<OrderTestData> CreateOrderAsync(string suffix)
    {
        var organization = Organization.Create(
            $"Orders {suffix}",
            $"orders-{suffix}-{Guid.NewGuid():N}",
            "PHP"
        );
        var userId = await RunAsUserAsync(
            $"order-{suffix}-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        var customer = CustomerProfile.Create(organization.Id, userId, "Order Customer");
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            customer.Id,
            null,
            "PHP",
            AddressJson,
            AddressJson
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Snapshot Product",
            "SNAPSHOT-SKU",
            125m,
            2,
            250m,
            null,
            20m,
            null
        );

        await AddAsync(organization);
        await AddAsync(customer);
        await AddAsync(order);

        return new OrderTestData(organization, order);
    }

    private sealed record OrderTestData(Organization Organization, Order Order);
}
