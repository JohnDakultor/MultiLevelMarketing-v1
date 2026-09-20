using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Commerce.Commands.CreateCheckout;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Referral;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Application.FunctionalTests.Referrals;

using static Infrastructure.TestApp;

public sealed class ReferralAttributionPolicyTests : TestBase
{
    [Test]
    public async Task ShouldPersistAttributionAndSnapshotItOnTheOrder()
    {
        var data = await CreateCheckoutDataAsync("persist");
        var capturedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await PrepareCartAsync(data, data.FirstAgent.ReferralCode, capturedAt);

        var orderId = await SendAsync(Checkout(data));

        var order = await FindAsync<Order>(orderId);
        order.ShouldNotBeNull();
        order.AttributedAgentId.ShouldBe(data.FirstAgent.Id);

        var attribution = await SingleAsync<ReferralAttribution>(candidate =>
            candidate.OrganizationId == data.Organization.Id
            && candidate.CustomerId == data.CustomerId
        );
        attribution.AgentId.ShouldBe(data.FirstAgent.Id);
        attribution.ReferralCode.ShouldBe(data.FirstAgent.ReferralCode);
        attribution.Source.ShouldBe(AttributionSource.ReferralLink);
        attribution.CapturedAt.ShouldBe(capturedAt, TimeSpan.FromMilliseconds(1));
        attribution.ExpiresAt.ShouldBe(capturedAt.AddDays(30), TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task ShouldRefreshOneDurableAttributionAcrossRepeatedCheckouts()
    {
        var data = await CreateCheckoutDataAsync("refresh");
        var firstCapture = DateTimeOffset.UtcNow.AddDays(-2);
        var refreshedCapture = DateTimeOffset.UtcNow.AddMinutes(-1);

        await PrepareCartAsync(data, data.FirstAgent.ReferralCode, firstCapture);
        await SendAsync(Checkout(data));
        await PrepareCartAsync(data, data.FirstAgent.ReferralCode, refreshedCapture);
        await SendAsync(Checkout(data));

        (await CountAsync<ReferralAttribution>()).ShouldBe(1);
        var attribution = await SingleAsync<ReferralAttribution>(candidate =>
            candidate.OrganizationId == data.Organization.Id
            && candidate.CustomerId == data.CustomerId
        );
        attribution.AgentId.ShouldBe(data.FirstAgent.Id);
        attribution.CapturedAt.ShouldBe(refreshedCapture, TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task ShouldRetainOriginalAgentAfterTheFirstCompletedPurchase()
    {
        var data = await CreateCheckoutDataAsync("locked");
        var existingCapture = DateTimeOffset.UtcNow.AddDays(-5);
        var existingContext = ReferralAttributionContext.Capture(
            data.FirstAgent.Id,
            data.FirstAgent.ReferralCode,
            existingCapture,
            AttributionSource.ReferralLink
        );
        var attribution = ReferralAttribution.Capture(
            data.Organization.Id,
            data.CustomerId,
            existingContext,
            existingCapture.AddDays(30)
        );
        var paidOrder = CreatePaidOrder(data);
        await AddAsync(attribution);
        await AddAsync(paidOrder);
        await PrepareCartAsync(
            data,
            data.SecondAgent.ReferralCode,
            DateTimeOffset.UtcNow.AddMinutes(-1)
        );

        var orderId = await SendAsync(Checkout(data));

        var order = await FindAsync<Order>(orderId);
        order.ShouldNotBeNull();
        order.AttributedAgentId.ShouldBe(data.FirstAgent.Id);
        var stored = await SingleAsync<ReferralAttribution>(candidate =>
            candidate.OrganizationId == data.Organization.Id
            && candidate.CustomerId == data.CustomerId
        );
        stored.AgentId.ShouldBe(data.FirstAgent.Id);
        stored.CapturedAt.ShouldBe(existingCapture, TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task ShouldIgnoreAnExpiredIncomingAttribution()
    {
        var data = await CreateCheckoutDataAsync("expired");
        await PrepareCartAsync(
            data,
            data.FirstAgent.ReferralCode,
            DateTimeOffset.UtcNow.AddDays(-31)
        );

        var orderId = await SendAsync(Checkout(data));

        var order = await FindAsync<Order>(orderId);
        order.ShouldNotBeNull();
        order.AttributedAgentId.ShouldBeNull();
        (await CountAsync<ReferralAttribution>()).ShouldBe(0);
    }

    private static async Task<CheckoutData> CreateCheckoutDataAsync(string suffix)
    {
        var organization = Organization.Create($"Checkout {suffix}", $"checkout-{suffix}", "USD");
        var userId = await RunAsUserAsync(
            $"customer-{suffix}-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        var customer = CustomerProfile.Create(organization.Id, userId, "Checkout Customer");
        var firstAgent = CreateActiveAgent(organization.Id, $"FIRST-{suffix}");
        var secondAgent = CreateActiveAgent(organization.Id, $"SECOND-{suffix}");
        var category = Category.Create(organization.Id, "Products", $"products-{suffix}");
        var product = Product.Create(
            organization.Id,
            category.Id,
            "Test Product",
            $"test-product-{suffix}",
            "A product used by checkout tests."
        );
        var variant = product.AddVariant($"SKU-{suffix}", 100m, 10m, 10);
        product.Publish();

        await AddAsync(organization);
        await AddAsync(customer);
        await AddAsync(firstAgent);
        await AddAsync(secondAgent);
        await AddAsync(category);
        await AddAsync(product);

        return new CheckoutData(
            organization,
            customer.Id,
            firstAgent,
            secondAgent,
            product,
            variant
        );
    }

    private static Agent CreateActiveAgent(Guid organizationId, string code)
    {
        var agent = Agent.Apply(
            organizationId,
            Guid.NewGuid().ToString(),
            code,
            $"REF-{code}",
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        return agent;
    }

    private static CreateCheckoutCommand Checkout(CheckoutData data) =>
        new(data.Organization.Id, Address(), Address());

    private static CheckoutAddressInput Address() =>
        new(
            "Checkout Customer",
            "+639171234567",
            "123 Test Street",
            null,
            "Test Barangay",
            "Manila",
            "Metro Manila",
            "1000",
            "PH"
        );

    private static async Task PrepareCartAsync(
        CheckoutData data,
        string referralCode,
        DateTimeOffset capturedAt
    )
    {
        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var cart = await context
                .Carts.Include(candidate => candidate.Items)
                .SingleOrDefaultAsync(candidate =>
                    candidate.OrganizationId == data.Organization.Id
                    && candidate.CustomerId == data.CustomerId
                );
            if (cart is null)
            {
                cart = Cart.Create(data.Organization.Id, data.CustomerId, null);
                context.Carts.Add(cart);
            }

            var agent =
                data.FirstAgent.ReferralCode == referralCode ? data.FirstAgent : data.SecondAgent;
            cart.AddItem(data.Variant.Id, 1);
            cart.ApplyReferral(
                agent.Id,
                referralCode,
                capturedAt,
                AttributionSource.ReferralLink.ToString()
            );
            await context.SaveChangesAsync();
            return true;
        });
    }

    private static Order CreatePaidOrder(CheckoutData data)
    {
        var order = Order.Create(
            data.Organization.Id,
            $"PAID-{Guid.NewGuid():N}",
            data.CustomerId,
            data.FirstAgent.Id,
            "USD",
            "{}",
            "{}"
        );
        order.AddItem(
            data.Product.Id,
            data.Variant.Id,
            data.Product.Name,
            data.Variant.Sku,
            data.Variant.Price,
            1,
            data.Variant.Price,
            null,
            data.Variant.BusinessVolume,
            null
        );
        order.MarkPaid(DateTimeOffset.UtcNow);
        return order;
    }

    private sealed record CheckoutData(
        Organization Organization,
        Guid CustomerId,
        Agent FirstAgent,
        Agent SecondAgent,
        Product Product,
        ProductVariant Variant
    );
}
