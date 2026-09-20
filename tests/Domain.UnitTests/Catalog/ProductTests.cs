using modular_mlm.Domain.Catalog;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Catalog;

public sealed class ProductTests
{
    [Test]
    public void UntrackedVariantRetainsStockKeepingChoice()
    {
        var product = Product.Create(Guid.NewGuid(), Guid.NewGuid(), "Product", "product", "");
        var variant = product.AddVariant(" sku ", 10m, 1m, 0, stockKeepingEnabled: false);
        variant.StockKeepingEnabled.ShouldBeFalse();
        variant.Sku.ShouldBe("SKU");
        variant.ReserveStock(100);
        variant.StockQuantity.ShouldBe(0);
    }

    [Test]
    public void UntrackedVariantRejectsOpeningStock()
    {
        var product = Product.Create(Guid.NewGuid(), Guid.NewGuid(), "Product", "product", "");
        Should.Throw<modular_mlm.Domain.Exceptions.DomainInvariantException>(() =>
            product.AddVariant("SKU", 10m, 1m, 5, stockKeepingEnabled: false)
        );
        product.Variants.ShouldBeEmpty();
    }

    [Test]
    public void DuplicateSkuWithWhitespaceIsRejected()
    {
        var product = Product.Create(Guid.NewGuid(), Guid.NewGuid(), "Product", "product", "");
        product.AddVariant("SKU", 10m, 1m, 0);
        Should.Throw<modular_mlm.Domain.Exceptions.DomainInvariantException>(() =>
            product.AddVariant(" sku ", 10m, 1m, 0)
        );
        product.Variants.Count.ShouldBe(1);
    }

    [Test]
    public void ProductCannotBePublishedWithoutVariant()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "product",
            "Description"
        );
        Should.Throw<Exception>(() => product.Publish());
    }

    [Test]
    public void ProductOwnsVariantStockBehavior()
    {
        var product = Product.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "product",
            "Description"
        );
        var variant = product.AddVariant("sku-1", 100m, 10m, 5);
        product.Publish();
        variant.ReserveStock(2);
        product.Status.ShouldBe(ProductStatus.Active);
        variant.StockQuantity.ShouldBe(5);
        variant.ReservedQuantity.ShouldBe(2);
        variant.AvailableQuantity.ShouldBe(3);
    }
}
