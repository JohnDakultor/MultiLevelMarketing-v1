using modular_mlm.Domain.Commerce;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Commerce;

public sealed class CartTests
{
    [Test]
    public void AnonymousCartCanBeAssignedToCustomer()
    {
        var cart = Cart.Create(Guid.NewGuid(), null, "anonymous-session");
        var customerId = Guid.NewGuid();

        cart.AssignToCustomer(customerId);

        cart.CustomerId.ShouldBe(customerId);
        cart.SessionId.ShouldBeNull();
    }

    [Test]
    public void CustomerCartMergesItemsAndReferralFromAnonymousCart()
    {
        var organizationId = Guid.NewGuid();
        var customerCart = Cart.Create(organizationId, Guid.NewGuid(), null);
        var anonymousCart = Cart.Create(organizationId, null, "anonymous-session");
        var sharedVariantId = Guid.NewGuid();
        var anonymousVariantId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var capturedAt = DateTimeOffset.UtcNow;

        customerCart.AddItem(sharedVariantId, 2);
        anonymousCart.AddItem(sharedVariantId, 3);
        anonymousCart.AddItem(anonymousVariantId, 4);
        anonymousCart.ApplyReferral(agentId, "REFERRAL", capturedAt, "Checkout");

        customerCart.MergeAnonymousCart(anonymousCart);

        customerCart
            .Items.Single(item => item.ProductVariantId == sharedVariantId)
            .Quantity.ShouldBe(5);
        customerCart
            .Items.Single(item => item.ProductVariantId == anonymousVariantId)
            .Quantity.ShouldBe(4);
        customerCart.AttributedAgentId.ShouldBe(agentId);
        customerCart.ReferralCode.ShouldBe("REFERRAL");
    }

    [Test]
    public void MergeCapsACombinedLineAtTheSupportedMaximum()
    {
        var organizationId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var customerCart = Cart.Create(organizationId, Guid.NewGuid(), null);
        var anonymousCart = Cart.Create(organizationId, null, "anonymous-session");
        customerCart.AddItem(variantId, 75);
        anonymousCart.AddItem(variantId, 50);

        customerCart.MergeAnonymousCart(anonymousCart);

        customerCart.Items.Single().Quantity.ShouldBe(Cart.MaximumQuantityPerLine);
    }
}
