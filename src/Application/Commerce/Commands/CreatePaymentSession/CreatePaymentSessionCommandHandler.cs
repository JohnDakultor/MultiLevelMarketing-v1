using modular_mlm.Application.Commerce.Commands.CreatePaymentSession.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Commands.CreatePaymentSession;

public sealed class CreatePaymentSessionCommandHandler(
    IApplicationDbContext db,
    IPaymentGateway gateway
) : IRequestHandler<CreatePaymentSessionCommand, PaymentSessionDto>
{
    public async Task<PaymentSessionDto> Handle(
        CreatePaymentSessionCommand request,
        CancellationToken cancellationToken
    )
    {
        var order = await db
            .Orders.Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.OrderId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (order is null)
            throw new KeyNotFoundException("Order was not found.");
        if (order.PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("The order has already been paid.");
        if (order.Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException("The order is not awaiting payment.");
        if (!string.Equals(order.Currency, "PHP", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PayMongo checkout currently requires PHP.");

        var payment = await db.Payments.SingleOrDefaultAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.OrderId == request.OrderId,
            cancellationToken
        );
        if (payment?.ProviderCheckoutSessionId is not null && payment.CheckoutUrl is not null)
            return new PaymentSessionDto(
                payment.Id,
                payment.ProviderCheckoutSessionId,
                payment.CheckoutUrl
            );

        if (payment is null)
        {
            payment = Payment.Initiate(
                request.OrganizationId,
                order.Id,
                "PayMongo",
                $"paymongo-checkout:{request.OrganizationId:N}:{order.Id:N}",
                order.GrandTotal,
                order.Currency
            );
            db.Payments.Add(payment);
            await db.SaveChangesAsync(cancellationToken);
        }

        var session = await gateway.CreateCheckoutAsync(
            new CreatePaymentCheckoutRequest(
                order.Id,
                order.OrderNumber,
                order
                    .Items.Select(item => new PaymentCheckoutLineItem(
                        item.ProductNameSnapshot,
                        ToMinorUnits(item.UnitPrice),
                        order.Currency,
                        item.Quantity
                    ))
                    .ToList(),
                request
                    .PaymentMethodTypes.Select(method => method.ToLowerInvariant())
                    .Distinct()
                    .ToList(),
                request.SuccessUrl,
                request.CancelUrl,
                payment.IdempotencyKey
            ),
            cancellationToken
        );
        payment.AttachCheckoutSession(session.Id, session.CheckoutUrl);
        await db.SaveChangesAsync(cancellationToken);
        return new PaymentSessionDto(payment.Id, session.Id, session.CheckoutUrl);
    }

    private static long ToMinorUnits(decimal amount)
    {
        var minorUnits = amount * 100m;
        if (minorUnits != decimal.Truncate(minorUnits))
            throw new InvalidOperationException(
                "Order prices must not contain fractional centavos."
            );
        return decimal.ToInt64(minorUnits);
    }
}
