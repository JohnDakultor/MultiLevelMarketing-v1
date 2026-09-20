using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Inventory.Commands.FinalizeOrderInventory;

public sealed class FinalizeOrderInventoryCommandHandler(
    IApplicationDbContext db,
    TimeProvider timeProvider
) : IRequestHandler<FinalizeOrderInventoryCommand, int>
{
    private static readonly Guid SystemActorId = new("00000000-0000-0000-0000-000000000001");

    public async Task<int> Handle(
        FinalizeOrderInventoryCommand request,
        CancellationToken cancellationToken
    )
    {
        var orderExists = await db.Orders.AnyAsync(
            order => order.Id == request.OrderId && order.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (!orderExists)
            throw new KeyNotFoundException("Order was not found in this organization.");

        var reservations = await db
            .InventoryReservations.Where(reservation =>
                reservation.OrganizationId == request.OrganizationId
                && reservation.OrderId == request.OrderId
                && reservation.Status == InventoryReservationStatus.Active
            )
            .OrderBy(reservation => reservation.Id)
            .ToListAsync(cancellationToken);
        if (reservations.Count == 0)
            return 0;

        var variantIds = reservations.Select(reservation => reservation.ProductVariantId).ToArray();
        var variants = await (
            from variant in db.ProductVariants
            join product in db.Products on variant.ProductId equals product.Id
            where
                product.OrganizationId == request.OrganizationId && variantIds.Contains(variant.Id)
            select variant
        ).ToDictionaryAsync(variant => variant.Id, cancellationToken);
        if (variants.Count != variantIds.Distinct().Count())
            throw new InvalidOperationException(
                "A reserved product variant is missing from this organization."
            );

        var now = timeProvider.GetUtcNow();
        var finalized = 0;
        foreach (var reservation in reservations)
        {
            if (!reservation.Finalize(now))
                continue;
            var variant = variants[reservation.ProductVariantId];
            var before = variant.StockQuantity;
            var expectedVersion = variant.Version;
            variant.FinalizeReservedStock(reservation.Quantity);
            db.InventoryAdjustments.Add(
                InventoryAdjustment.Record(
                    request.OrganizationId,
                    variant.Id,
                    -reservation.Quantity,
                    InventoryAdjustmentType.SaleFinalization,
                    $"Inventory finalized for order {request.OrderId}.",
                    $"reservation-finalize:{reservation.Id:N}",
                    expectedVersion,
                    before,
                    variant.StockQuantity,
                    now,
                    SystemActorId
                )
            );
            finalized++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return finalized;
    }
}
