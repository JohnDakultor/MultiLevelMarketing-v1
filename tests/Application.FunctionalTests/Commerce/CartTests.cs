using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Commerce.Commands.AddCartItem;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class CartTests : TestBase
{
    [Test]
    public async Task AuthenticatedCustomerCanAddToOwnedCartWithoutSendingCustomerId()
    {
        var data = await CreateCatalogAsync("owned", stockQuantity: 10);

        var cartId = await SendAsync(
            new AddCartItemCommand(data.Organization.Id, data.Variant.Id, 2)
        );
        var sameCartId = await SendAsync(
            new AddCartItemCommand(data.Organization.Id, data.Variant.Id, 3)
        );

        sameCartId.ShouldBe(cartId);
        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var cart = await context
                .Carts.AsNoTracking()
                .Include(candidate => candidate.Items)
                .SingleAsync(candidate => candidate.Id == cartId);

            cart.CustomerId.ShouldBe(data.Customer!.Id);
            cart.SessionId.ShouldBeNull();
            cart.Items.Single().ProductVariantId.ShouldBe(data.Variant.Id);
            cart.Items.Single().Quantity.ShouldBe(5);
            return true;
        });
    }

    [Test]
    public async Task CannotAddVariantFromAnotherOrganization()
    {
        var owned = await CreateCatalogAsync("tenant-a", stockQuantity: 10);
        var foreign = await CreateCatalogAsync("tenant-b", stockQuantity: 10, createUser: false);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            SendAsync(new AddCartItemCommand(owned.Organization.Id, foreign.Variant.Id, 1))
        );
    }

    [Test]
    public async Task CannotExceedAvailableStock()
    {
        var data = await CreateCatalogAsync("stock", stockQuantity: 2);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(new AddCartItemCommand(data.Organization.Id, data.Variant.Id, 3))
        );

        exception.Message.ShouldContain("Available stock: 2");
        (await CountAsync<Cart>()).ShouldBe(0);
    }

    private static async Task<CartTestData> CreateCatalogAsync(
        string suffix,
        int stockQuantity,
        bool createUser = true
    )
    {
        var organization = Organization.Create(
            $"Cart {suffix}",
            $"cart-{suffix}-{Guid.NewGuid():N}",
            "PHP"
        );
        CustomerProfile? customer = null;
        if (createUser)
        {
            var userId = await RunAsUserAsync(
                $"cart-{suffix}-{Guid.NewGuid():N}@local",
                "Testing1234!",
                []
            );
            customer = CustomerProfile.Create(organization.Id, userId, "Cart Customer");
        }

        var category = Category.Create(
            organization.Id,
            "Products",
            $"products-{suffix}-{Guid.NewGuid():N}"
        );
        var product = Product.Create(
            organization.Id,
            category.Id,
            "Cart Product",
            $"cart-product-{suffix}-{Guid.NewGuid():N}",
            "Product used by cart tests."
        );
        var variant = product.AddVariant(
            $"SKU-{suffix}-{Guid.NewGuid():N}",
            125m,
            10m,
            stockQuantity
        );
        product.Publish();

        await AddAsync(organization);
        if (customer is not null)
            await AddAsync(customer);
        await AddAsync(category);
        await AddAsync(product);

        return new CartTestData(organization, customer, variant);
    }

    private sealed record CartTestData(
        Organization Organization,
        CustomerProfile? Customer,
        ProductVariant Variant
    );
}
