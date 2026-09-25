using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;

namespace modular_mlm.Application.Commerce.Commands.MarkOrderShipped;

public sealed class MarkOrderShippedCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<MarkOrderShippedCommand>
{
    public async Task Handle(MarkOrderShippedCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(candidate => candidate.Items).SingleOrDefaultAsync(
            candidate => candidate.OrganizationId == request.OrganizationId && candidate.Id == request.OrderId,
            cancellationToken
        );
        if (order is null)
            throw new KeyNotFoundException("Order was not found.");

        var beforeJson = AuditJson.Serialize(new { order.Status });
        order.Ship(timeProvider.GetUtcNow(), request.Carrier, request.TrackingNumber);
        var audit = AuditCoverageMap.OrderShipped;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            order.Id,
            beforeJson,
            AuditJson.Serialize(new { order.Status, order.ShippedAt, order.ShippingCarrier, order.TrackingNumber }),
            reason: null
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
