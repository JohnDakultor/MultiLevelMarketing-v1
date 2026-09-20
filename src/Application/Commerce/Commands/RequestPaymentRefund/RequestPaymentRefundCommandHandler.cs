using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Commands.RequestPaymentRefund;

public sealed class RequestPaymentRefundCommandHandler(
    IUser currentUser,
    IAdministratorAccountService administratorAccounts,
    IApplicationDbContext db,
    IPaymentGateway gateway,
    TimeProvider clock
) : IRequestHandler<RequestPaymentRefundCommand, Guid>
{
    public async Task<Guid> Handle(
        RequestPaymentRefundCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (
            !await administratorAccounts.CanManageOrganizationAsync(
                userId,
                request.OrganizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();

        var payment = await db.Payments.SingleOrDefaultAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.OrderId == request.OrderId,
            cancellationToken
        );
        if (payment is null)
            throw new KeyNotFoundException("Payment was not found.");
        if (payment.ProviderPaymentId is null)
            throw new InvalidOperationException("The provider payment has not been confirmed.");
        var refundableAmount = payment.Amount - payment.RefundedAmount;
        if (request.Amount != refundableAmount)
            throw new InvalidOperationException(
                "Only a full refund of the remaining payment is supported until item-level refund allocation is implemented."
            );

        var refund = PaymentRefund.Request(
            request.OrganizationId,
            payment.Id,
            request.OrderId,
            request.Amount,
            payment.Currency,
            request.Reason,
            clock.GetUtcNow()
        );
        db.PaymentRefunds.Add(refund);
        await db.SaveChangesAsync(cancellationToken);

        var result = await gateway.RefundAsync(
            new CreatePaymentRefundRequest(
                payment.ProviderPaymentId,
                ToMinorUnits(refund.Amount),
                refund.Reason,
                refund.IdempotencyKey
            ),
            cancellationToken
        );
        refund.AttachProviderResult(result.ProviderRefundId, result.Status, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return refund.Id;
    }

    private static long ToMinorUnits(decimal amount) => decimal.ToInt64(amount * 100m);
}
