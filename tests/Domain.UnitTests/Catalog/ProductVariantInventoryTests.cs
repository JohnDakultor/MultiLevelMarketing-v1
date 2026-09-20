using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Catalog;

public sealed class ProductVariantInventoryTests
{
    [Test]
    public void AdjustOnHandAppliesDeltaAndAdvancesVersion()
    {
        var variant = CreateVariant(stockQuantity: 10);
        var versionBefore = variant.Version;

        variant.AdjustOnHand(4);

        variant.StockQuantity.ShouldBe(14);
        variant.Version.ShouldBe(versionBefore + 1);
    }

    [Test]
    public void AdjustOnHandRejectsNegativeResultWithoutChangingState()
    {
        var variant = CreateVariant(stockQuantity: 3);
        var versionBefore = variant.Version;

        Should.Throw<DomainInvariantException>(() => variant.AdjustOnHand(-4));

        variant.StockQuantity.ShouldBe(3);
        variant.Version.ShouldBe(versionBefore);
    }

    [Test]
    public void InventoryAdjustmentRecordsTenantActorAndEvent()
    {
        var organizationId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.Parse("2026-09-08T10:00:00Z");

        var adjustment = InventoryAdjustment.Record(
            organizationId,
            variantId,
            5,
            InventoryAdjustmentType.Receipt,
            "Warehouse receipt",
            " RECEIPT-001 ",
            3,
            10,
            15,
            occurredAt,
            actorId
        );

        adjustment.OrganizationId.ShouldBe(organizationId);
        adjustment.ActorUserId.ShouldBe(actorId);
        adjustment.IdempotencyKey.ShouldBe("receipt-001");
        adjustment.DomainEvents.ShouldHaveSingleItem();
    }

    private static ProductVariant CreateVariant(int stockQuantity)
    {
        var product = Product.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Product",
            "product",
            "Description"
        );
        return product.AddVariant("sku-1", 100m, 10m, stockQuantity);
    }
}
