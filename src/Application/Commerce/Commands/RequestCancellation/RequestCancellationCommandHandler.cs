using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Services;

namespace modular_mlm.Application.Commerce.Commands.RequestCancellation;

public sealed class RequestCancellationCommandHandler(
    IApplicationDbContext db,
    IUser currentUser,
    OrderCancellationPolicy cancellationPolicy,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<RequestCancellationCommand>
{
    public async Task Handle(
        RequestCancellationCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var customerId = await db
            .CustomerProfiles.AsNoTracking()
            .Where(customer =>
                customer.OrganizationId == request.OrganizationId && customer.UserId == userId
            )
            .Select(customer => (Guid?)customer.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!customerId.HasValue)
            throw new KeyNotFoundException(
                "A customer profile was not found for the current user in this organization."
            );

        var order = await db
            .Orders.Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.OrderId
                    && candidate.OrganizationId == request.OrganizationId
                    && candidate.CustomerId == customerId.Value,
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

        var reservations = await db
            .InventoryReservations.Where(reservation =>
                reservation.OrganizationId == request.OrganizationId
                && reservation.OrderId == order.Id
                && reservation.Status == InventoryReservationStatus.Active
            )
            .ToListAsync(cancellationToken);
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

        var beforeJson = AuditJson.Serialize(new { order.Status, order.PaymentStatus });
        order.Cancel();
        foreach (var reservation in reservations)
        {
            if (reservation.Release(request.Reason, timeProvider.GetUtcNow()))
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
            request.Reason.Trim()
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}
