using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Catalog.Commands.AdjustInventory;
using modular_mlm.Application.Catalog.Queries.GetAdminCategories;
using modular_mlm.Application.Catalog.Queries.GetAdminProduct;
using modular_mlm.Application.Inventory.Commands.FinalizeOrderInventory;
using modular_mlm.Application.Inventory.Commands.ReleaseOrderInventory;
using modular_mlm.Application.Inventory.Queries.GetInventory;
using modular_mlm.Application.Inventory.Queries.GetInventoryHistory;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Application.FunctionalTests.Inventory;

using static Infrastructure.TestApp;

public sealed class InventoryReservationLifecycleTests : TestBase
{
    [Test]
    public async Task CancellationReleaseRestoresAvailabilityExactlyOnce()
    {
        var data = await SeedReservationAsync();

        (
            await SendAsync(
                new ReleaseOrderInventoryCommand(
                    data.OrganizationId,
                    data.OrderId,
                    "Customer cancellation"
                )
            )
        ).ShouldBe(1);
        (
            await SendAsync(
                new ReleaseOrderInventoryCommand(
                    data.OrganizationId,
                    data.OrderId,
                    "Customer cancellation"
                )
            )
        ).ShouldBe(0);

        var variant = await SingleAsync<ProductVariant>(candidate =>
            candidate.Id == data.VariantId
        );
        variant.StockQuantity.ShouldBe(10);
        variant.ReservedQuantity.ShouldBe(0);
        var reservation = await SingleAsync<InventoryReservation>(candidate =>
            candidate.OrderId == data.OrderId
        );
        reservation.Status.ShouldBe(InventoryReservationStatus.Released);
    }

    [Test]
    public async Task ConfirmedPaymentFinalizesInventoryExactlyOnce()
    {
        var data = await SeedReservationAsync();

        (
            await SendAsync(new FinalizeOrderInventoryCommand(data.OrganizationId, data.OrderId))
        ).ShouldBe(1);
        (
            await SendAsync(new FinalizeOrderInventoryCommand(data.OrganizationId, data.OrderId))
        ).ShouldBe(0);

        var variant = await SingleAsync<ProductVariant>(candidate =>
            candidate.Id == data.VariantId
        );
        variant.StockQuantity.ShouldBe(8);
        variant.ReservedQuantity.ShouldBe(0);
        (
            await CountAsync<InventoryAdjustment>(adjustment =>
                adjustment.ProductVariantId == data.VariantId
                && adjustment.AdjustmentType == InventoryAdjustmentType.SaleFinalization
            )
        ).ShouldBe(1);
    }

    [Test]
    public async Task AdministratorCanAdjustAndInspectTenantInventory()
    {
        var data = await SeedReservationAsync();
        await RunAsAdministratorAsync(data.OrganizationId);

        var adjustmentId = await SendAsync(
            new AdjustInventoryCommand(
                data.OrganizationId,
                data.VariantId,
                5,
                InventoryAdjustmentType.Receipt,
                "Warehouse receipt",
                "inventory-test-receipt",
                2
            )
        );

        var inventory = await SendAsync(new GetInventoryQuery(data.OrganizationId));
        var item = inventory.Items.ShouldHaveSingleItem();
        item.OnHand.ShouldBe(15);
        item.Reserved.ShouldBe(2);
        item.Available.ShouldBe(13);

        var history = await SendAsync(
            new GetInventoryHistoryQuery(data.OrganizationId, data.VariantId)
        );
        history.Items.ShouldHaveSingleItem().Id.ShouldBe(adjustmentId);

        var product = await SendAsync(
            new GetAdminProductQuery(data.OrganizationId, data.ProductId)
        );
        product.Variants.ShouldHaveSingleItem().Version.ShouldBe(3);

        var categories = await SendAsync(new GetAdminCategoriesQuery(data.OrganizationId));
        categories.Items.ShouldHaveSingleItem().ProductCount.ShouldBe(1);
    }

    private static async Task<ReservationData> SeedReservationAsync()
    {
        return await TestApp.ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var organization = Organization.Create(
                "Inventory test",
                $"inventory-{Guid.NewGuid():N}",
                "PHP"
            );
            var category = Category.Create(
                organization.Id,
                "Products",
                $"products-{Guid.NewGuid():N}"
            );
            var product = Product.Create(
                organization.Id,
                category.Id,
                "Tracked product",
                $"tracked-{Guid.NewGuid():N}",
                ""
            );
            var variant = product.AddVariant($"SKU-{Guid.NewGuid():N}", 100m, 10m, 10);
            var order = Order.Create(
                organization.Id,
                $"ORD-{Guid.NewGuid():N}",
                Guid.NewGuid(),
                null,
                "PHP",
                "{}",
                "{}"
            );
            order.AddItem(
                product.Id,
                variant.Id,
                product.Name,
                variant.Sku,
                variant.Price,
                2,
                0,
                null,
                variant.BusinessVolume,
                null
            );
            var now = DateTimeOffset.UtcNow;
            variant.ReserveStock(2);
            var reservation = InventoryReservation.Reserve(
                organization.Id,
                order.Id,
                variant.Id,
                2,
                now,
                now.AddMinutes(30)
            );

            db.AddRange(organization, category, product, order, reservation);
            await db.SaveChangesAsync();
            return new ReservationData(organization.Id, order.Id, product.Id, variant.Id);
        });
    }

    private sealed record ReservationData(
        Guid OrganizationId,
        Guid OrderId,
        Guid ProductId,
        Guid VariantId
    );
}
