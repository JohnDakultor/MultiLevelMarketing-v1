using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Commerce.Queries.GetCart;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class CustomerCartLifecycleTests : TestBase
{
    [Test]
    public async Task FirstAuthenticatedCartReadClaimsAnonymousCartAndClearsCookie()
    {
        var organization = Organization.Create(
            "Cart Lifecycle",
            $"cart-lifecycle-{Guid.NewGuid():N}",
            "PHP"
        );
        var category = Category.Create(organization.Id, "Products", $"products-{Guid.NewGuid():N}");
        var product = Product.Create(
            organization.Id,
            category.Id,
            "Lifecycle Product",
            $"lifecycle-product-{Guid.NewGuid():N}",
            "Product used to verify anonymous cart conversion."
        );
        var variant = product.AddVariant($"SKU-{Guid.NewGuid():N}", 100m, 10m, 20);
        product.Publish();
        await AddAsync(organization);
        await AddAsync(category);
        await AddAsync(product);

        using var client = FunctionalTestSetup.CreateClient();
        using var anonymousResponse = await client.PostAsJsonAsync(
            $"/api/organizations/{organization.Id}/cart/items",
            new { productVariantId = variant.Id, quantity = 2 }
        );
        anonymousResponse.EnsureSuccessStatusCode();
        var cartCookie = anonymousResponse
            .Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .Single(value =>
                value.StartsWith("__Host-modular_mlm.cart=", StringComparison.Ordinal)
            );

        var email = $"cart-owner-{Guid.NewGuid():N}@local";
        const string password = "Testing1234!";
        var userId = await RunAsUserAsync(email, password, []);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginAsync(client, email, password)
        );
        client.DefaultRequestHeaders.Add("Cookie", cartCookie);

        using var authenticatedResponse = await client.GetAsync(
            $"/api/organizations/{organization.Id}/cart"
        );
        authenticatedResponse.EnsureSuccessStatusCode();
        var responseCart = await authenticatedResponse.Content.ReadFromJsonAsync<CartDto>();

        responseCart.ShouldNotBeNull();
        responseCart.HasAnonymousSession.ShouldBeFalse();
        responseCart.Items.Single().Quantity.ShouldBe(2);
        authenticatedResponse
            .Headers.GetValues("Set-Cookie")
            .ShouldContain(value =>
                value.StartsWith("__Host-modular_mlm.cart=", StringComparison.Ordinal)
                && value.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase)
            );

        await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var customer = await db.CustomerProfiles.SingleAsync(profile =>
                profile.OrganizationId == organization.Id && profile.UserId == userId
            );
            var persistedCart = await db.Carts.Include(cart => cart.Items).SingleAsync();
            persistedCart.CustomerId.ShouldBe(customer.Id);
            persistedCart.SessionId.ShouldBeNull();
            persistedCart.Items.Single().Quantity.ShouldBe(2);
            return true;
        });
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
