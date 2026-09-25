using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;

namespace modular_mlm.Application.Commerce.Commands.MarkOrderDelivered;

public sealed class MarkOrderDeliveredCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<MarkOrderDeliveredCommand>
{
    public async Task Handle(MarkOrderDeliveredCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(candidate => candidate.Items).SingleOrDefaultAsync(
            candidate => candidate.OrganizationId == request.OrganizationId && candidate.Id == request.OrderId,
            cancellationToken
        );
        if (order is null)
            throw new KeyNotFoundException("Order was not found.");

        var beforeJson = AuditJson.Serialize(new { order.Status, order.DeliveredAt });
        order.Deliver(timeProvider.GetUtcNow());
        var audit = AuditCoverageMap.OrderDelivered;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            order.Id,
            beforeJson,
            AuditJson.Serialize(new { order.Status, order.DeliveredAt }),
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
