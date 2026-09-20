using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Inventory.Commands.FinalizeOrderInventory;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Commands.HandlePaymentWebhook;

public sealed class HandlePaymentWebhookCommandHandler(
    IApplicationDbContext db,
    TimeProvider clock,
    ISender sender
) : IRequestHandler<HandlePaymentWebhookCommand, bool>
{
    public async Task<bool> Handle(
        HandlePaymentWebhookCommand request,
        CancellationToken cancellationToken
    )
    {
        if (
            await db.PaymentWebhookReceipts.AnyAsync(
                receipt =>
                    receipt.Provider == "PayMongo" && receipt.ProviderEventId == request.EventId,
                cancellationToken
            )
        )
            return false;

        var payment = await db.Payments.SingleOrDefaultAsync(
            candidate => candidate.ProviderCheckoutSessionId == request.CheckoutSessionId,
            cancellationToken
        );
        if (payment is null)
            throw new KeyNotFoundException("The PayMongo checkout session is unknown.");
        var order = await db
            .Orders.Include(candidate => candidate.Items)
            .SingleAsync(
                candidate =>
                    candidate.Id == payment.OrderId
                    && candidate.OrganizationId == payment.OrganizationId,
                cancellationToken
            );
        if (!string.Equals(order.OrderNumber, request.ReferenceNumber, StringComparison.Ordinal))
            throw new InvalidOperationException("The PayMongo reference does not match the order.");
        if (request.EventType != "checkout_session.payment.paid")
            return false;

        db.PaymentWebhookReceipts.Add(
            PaymentWebhookReceipt.Record(
                "PayMongo",
                request.EventId,
                request.EventType,
                request.PayloadHash,
                clock.GetUtcNow()
            )
        );
        payment.MarkPaid(request.ProviderPaymentId, request.OccurredAt);
        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            order.MarkPaid(request.OccurredAt);
            await sender.Send(
                new FinalizeOrderInventoryCommand(order.OrganizationId, order.Id),
                cancellationToken
            );
        }
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
