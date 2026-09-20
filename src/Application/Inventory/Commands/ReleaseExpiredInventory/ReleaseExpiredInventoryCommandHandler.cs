using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Inventory.Commands.ReleaseExpiredInventory;

public sealed class ReleaseExpiredInventoryCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ReleaseExpiredInventoryCommand, ReleaseExpiredInventoryResult>
{
    public async Task<ReleaseExpiredInventoryResult> Handle(
        ReleaseExpiredInventoryCommand request,
        CancellationToken cancellationToken
    )
    {
        var reservations = await db
            .InventoryReservations.Where(reservation =>
                reservation.OrganizationId == request.OrganizationId
                && reservation.Status == InventoryReservationStatus.Active
                && reservation.ExpiresAt <= request.Now
            )
            .OrderBy(reservation => reservation.ExpiresAt)
            .ThenBy(reservation => reservation.Id)
            .Take(request.BatchSize)
            .ToListAsync(cancellationToken);
        if (reservations.Count == 0)
            return new ReleaseExpiredInventoryResult(0, 0, 0);

        var variantIds = reservations
            .Select(reservation => reservation.ProductVariantId)
            .Distinct()
            .ToArray();
        var variants = await (
            from variant in db.ProductVariants
            join product in db.Products on variant.ProductId equals product.Id
            where
                product.OrganizationId == request.OrganizationId && variantIds.Contains(variant.Id)
            select variant
        ).ToDictionaryAsync(variant => variant.Id, cancellationToken);
        if (variants.Count != variantIds.Length)
            throw new InvalidOperationException(
                "A reserved product variant is missing from this organization."
            );

        var orderIds = reservations.Select(reservation => reservation.OrderId).Distinct().ToArray();
        var orders = await db
            .Orders.Include(order => order.Items)
            .Where(order =>
                order.OrganizationId == request.OrganizationId && orderIds.Contains(order.Id)
            )
            .ToDictionaryAsync(order => order.Id, cancellationToken);

        var released = 0;
        foreach (var reservation in reservations)
        {
            if (!reservation.Release("Inventory reservation expired.", request.Now))
                continue;
            variants[reservation.ProductVariantId].ReleaseReservedStock(reservation.Quantity);
            released++;
        }

        foreach (var order in orders.Values)
        {
            if (
                order.Status == OrderStatus.PendingPayment
                && order.PaymentStatus == PaymentStatus.Pending
            )
                order.Cancel();
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new ReleaseExpiredInventoryResult(reservations.Count, released, 0);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new ReleaseExpiredInventoryResult(reservations.Count, 0, reservations.Count);
        }
    }
}
