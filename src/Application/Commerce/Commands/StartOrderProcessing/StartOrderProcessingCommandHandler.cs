using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;

namespace modular_mlm.Application.Commerce.Commands.StartOrderProcessing;

public sealed class StartOrderProcessingCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<StartOrderProcessingCommand>
{
    public async Task Handle(StartOrderProcessingCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(candidate => candidate.Items).SingleOrDefaultAsync(
            candidate => candidate.OrganizationId == request.OrganizationId && candidate.Id == request.OrderId,
            cancellationToken
        );
        if (order is null)
            throw new KeyNotFoundException("Order was not found.");

        var beforeJson = AuditJson.Serialize(new { order.Status });
        order.StartProcessing();

        var audit = AuditCoverageMap.OrderProcessingStarted;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            order.Id,
            beforeJson,
            AuditJson.Serialize(new { order.Status }),
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
