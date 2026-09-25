using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Services;

namespace modular_mlm.Application.Commerce.Commands.AdminCancelOrder;

public sealed class AdminCancelOrderCommandHandler(
    IApplicationDbContext db,
    OrderCancellationPolicy cancellationPolicy,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<AdminCancelOrderCommand>
{
    public async Task Handle(AdminCancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(candidate => candidate.Items).SingleOrDefaultAsync(
            candidate => candidate.OrganizationId == request.OrganizationId && candidate.Id == request.OrderId,
            cancellationToken
        );
        if (order is null)
            throw new KeyNotFoundException("Order was not found.");

        var decision = cancellationPolicy.Evaluate(
            order.Status,
            order.PaymentStatus,
            order.Items.Select(item => item.FulfillmentStatus)
        );
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Explanation);

        var reservations = await db.InventoryReservations.Where(reservation =>
            reservation.OrganizationId == request.OrganizationId
            && reservation.OrderId == order.Id
            && reservation.Status == InventoryReservationStatus.Active
        ).ToListAsync(cancellationToken);
        var variantIds = reservations.Select(reservation => reservation.ProductVariantId).Distinct().ToArray();
        var variants = await (
            from variant in db.ProductVariants
            join product in db.Products on variant.ProductId equals product.Id
            where product.OrganizationId == request.OrganizationId && variantIds.Contains(variant.Id)
            select variant
        ).ToDictionaryAsync(variant => variant.Id, cancellationToken);
        if (variants.Count != variantIds.Length)
            throw new InvalidOperationException(
                "An inventory reservation references a missing tenant product variant."
            );

        var reason = request.Reason.Trim();
        var beforeJson = AuditJson.Serialize(new { order.Status, order.PaymentStatus });
        order.Cancel();
        var releasedAt = timeProvider.GetUtcNow();
        foreach (var reservation in reservations)
        {
            if (reservation.Release(reason, releasedAt))
                variants[reservation.ProductVariantId].ReleaseReservedStock(reservation.Quantity);
        }

        var audit = AuditCoverageMap.OrderCancelled;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            order.Id,
            beforeJson,
            AuditJson.Serialize(new { order.Status, order.PaymentStatus }),
            reason
        );
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DatabaseConcurrencyConflictException("order", order.Id.ToString());
        }
    }
}
