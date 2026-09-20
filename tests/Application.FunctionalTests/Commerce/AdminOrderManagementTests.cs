using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails;
using modular_mlm.Application.Commerce.Queries.GetAdminOrders;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class AdminOrderManagementTests : TestBase
{
    private const string AddressJson = """
        {
          "recipientName": "Admin Order Customer",
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
    public async Task AdministratorCanSearchAndPageOnlyOwnedOrganizationOrders()
    {
        var owned = Organization.Create(
            "Admin Orders Owned",
            $"admin-orders-owned-{Guid.NewGuid():N}",
            "PHP"
        );
        var foreign = Organization.Create(
            "Admin Orders Foreign",
            $"admin-orders-foreign-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(owned);
        await AddAsync(foreign);
        await CreateOrderAsync(owned.Id, "Green Customer", "ORD-GREEN-001");
        await CreateOrderAsync(owned.Id, "Green Customer", "ORD-GREEN-002");
        await CreateOrderAsync(foreign.Id, "Green Customer", "ORD-GREEN-FOREIGN");
        await RunAsAdministratorAsync(owned.Id);

        var page = await SendAsync(
            new GetAdminOrdersQuery(
                owned.Id,
                2,
                1,
                "green",
                OrderStatus.PendingPayment,
                PaymentStatus.Pending,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1)
            )
        );

        page.TotalCount.ShouldBe(2);
        page.TotalPages.ShouldBe(2);
        page.HasPreviousPage.ShouldBeTrue();
        page.HasNextPage.ShouldBeFalse();
        page.Items.Count.ShouldBe(1);
        page.Items[0].OrderNumber.ShouldNotBe("ORD-GREEN-FOREIGN");
        page.Items[0].CustomerDisplayName.ShouldBe("Green Customer");
    }

    [Test]
    public async Task AdministratorOrderDetailsIncludePaymentsAndRefundHistory()
    {
        var organization = Organization.Create(
            "Admin Order Details",
            $"admin-order-details-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var order = await CreateOrderAsync(organization.Id, "Refund Customer", "ORD-REFUND-001");
        var payment = Payment.Initiate(
            organization.Id,
            order.Id,
            "PayMongo",
            $"payment-{Guid.NewGuid():N}",
            order.GrandTotal,
            "PHP"
        );
        await AddAsync(payment);
        var paymentRefund = PaymentRefund.Request(
            organization.Id,
            payment.Id,
            order.Id,
            50m,
            "PHP",
            "Damaged item",
            DateTimeOffset.UtcNow
        );
        await AddAsync(paymentRefund);
        var orderItem = order.Items.Single();
        var itemRefund = OrderItemRefund.Create(
            organization.Id,
            order.Id,
            orderItem.Id,
            paymentRefund.Id,
            1m,
            50m,
            50m,
            5m,
            orderItem.Quantity
        );
        await AddAsync(itemRefund);
        await RunAsAdministratorAsync(organization.Id);

        var details = await SendAsync(new GetAdminOrderDetailsQuery(organization.Id, order.Id));

        details.ShouldNotBeNull();
        details.CustomerDisplayName.ShouldBe("Refund Customer");
        details.Items.Count.ShouldBe(1);
        details.Payments.Count.ShouldBe(1);
        details.Payments[0].Provider.ShouldBe("PayMongo");
        details.Payments[0].Refunds.Count.ShouldBe(1);
        details.Payments[0].Refunds[0].Reason.ShouldBe("Damaged item");
        details.ItemRefunds.Count.ShouldBe(1);
        details.ItemRefunds[0].OrderItemId.ShouldBe(orderItem.Id);
    }

    [Test]
    public async Task ForeignOrganizationAdministratorCannotListOrReadOrders()
    {
        var owned = Organization.Create(
            "Admin Scope Owned",
            $"admin-scope-owned-{Guid.NewGuid():N}",
            "PHP"
        );
        var foreign = Organization.Create(
            "Admin Scope Foreign",
            $"admin-scope-foreign-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(owned);
        await AddAsync(foreign);
        var foreignOrder = await CreateOrderAsync(
            foreign.Id,
            "Foreign Customer",
            "ORD-FOREIGN-001"
        );
        await RunAsAdministratorAsync(owned.Id);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetAdminOrdersQuery(foreign.Id, 1, 20, null, null, null, null, null))
        );
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetAdminOrderDetailsQuery(foreign.Id, foreignOrder.Id))
        );
    }

    [Test]
    public async Task AdminOrderRoutesEnforceAuthenticationRoleAndTenantScope()
    {
        var organization = Organization.Create(
            "Admin Order Route",
            $"admin-order-route-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await CreateOrderAsync(organization.Id, "Route Customer", "ORD-ROUTE-001");

        using var anonymousClient = FunctionalTestSetup.CreateClient();
        using var unauthorized = await anonymousClient.GetAsync(
            $"/api/organizations/{organization.Id}/admin/orders"
        );
        unauthorized.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        const string customerEmail = "admin-order-route-customer@local";
        const string customerPassword = "Testing1234!";
        await RunAsUserAsync(customerEmail, customerPassword, []);
        using var customerClient = FunctionalTestSetup.CreateClient();
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginAsync(customerClient, customerEmail, customerPassword)
        );
        using var forbidden = await customerClient.GetAsync(
            $"/api/organizations/{organization.Id}/admin/orders"
        );
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await RunAsAdministratorAsync(organization.Id);
        using var administratorClient = FunctionalTestSetup.CreateClient();
        administratorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginAsync(administratorClient, "administrator@local", "Administrator1234!")
        );
        using var success = await administratorClient.GetAsync(
            $"/api/organizations/{organization.Id}/admin/orders?page=1&pageSize=20"
        );
        success.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await success.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().ShouldBe(1);
    }

    private static async Task<Order> CreateOrderAsync(
        Guid organizationId,
        string customerName,
        string orderNumber
    )
    {
        var customer = CustomerProfile.Create(
            organizationId,
            Guid.NewGuid().ToString(),
            customerName
        );
        await AddAsync(customer);
        var order = Order.Create(
            organizationId,
            orderNumber,
            customer.Id,
            null,
            "PHP",
            AddressJson,
            AddressJson
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Admin Order Product",
            $"SKU-{Guid.NewGuid():N}",
            100m,
            2,
            200m,
            null,
            20m,
            null
        );
        await AddAsync(order);
        return order;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=false&useSessionCookies=false",
            new { email, password }
        );
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login did not return an access token.");
    }
}
