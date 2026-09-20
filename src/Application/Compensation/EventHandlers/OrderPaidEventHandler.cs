using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Commands.ProcessPaidOrderCommissions;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Events;

namespace modular_mlm.Application.Compensation.EventHandlers;

public sealed class OrderPaidEventHandler(IApplicationDbContext db, ISender sender)
    : INotificationHandler<OrderPaidEvent>
{
    public async Task Handle(OrderPaidEvent notification, CancellationToken cancellationToken)
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(candidate =>
                candidate.Id == notification.OrderId
                && candidate.OrganizationId == notification.OrganizationId
            )
            .Select(candidate => new
            {
                candidate.OrganizationId,
                candidate.PaymentStatus,
                candidate.PaidAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
            throw new KeyNotFoundException("The paid order referenced by the event was not found.");
        if (order.PaymentStatus != PaymentStatus.Paid || order.PaidAt is null)
            throw new InvalidOperationException(
                "The order-paid event cannot be processed before provider-verified payment."
            );

        await sender.Send(
            new ProcessPaidOrderCommissionsCommand(order.OrganizationId, notification.OrderId),
            cancellationToken
        );
    }
}
