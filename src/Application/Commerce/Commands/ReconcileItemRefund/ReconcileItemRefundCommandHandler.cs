using modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Commerce.Commands.ReconcileItemRefund;

public sealed class ReconcileItemRefundCommandHandler(
    IUser currentUser,
    IAdministratorAccountService administratorAccounts,
    IApplicationDbContext db,
    IPaymentGateway gateway,
    ISender sender,
    TimeProvider clock
) : IRequestHandler<ReconcileItemRefundCommand, bool>
{
    public async Task<bool> Handle(
        ReconcileItemRefundCommand request,
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

        var itemRefund = await db.OrderItemRefunds.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.OrderItemRefundId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (itemRefund is null)
            throw new KeyNotFoundException("Order item refund was not found.");
        var paymentRefund = await db.PaymentRefunds.SingleAsync(
            candidate => candidate.Id == itemRefund.PaymentRefundId,
            cancellationToken
        );
        var payment = await db.Payments.SingleAsync(
            candidate => candidate.Id == paymentRefund.PaymentId,
            cancellationToken
        );
        if (payment.ProviderPaymentId is null)
            throw new InvalidOperationException("Payment has no provider identifier.");

        var provider = await gateway.GetPaymentAsync(payment.ProviderPaymentId, cancellationToken);
        var providerRefunded = provider.RefundedAmountInMinorUnits / 100m;
        if (providerRefunded < payment.RefundedAmount + paymentRefund.Amount)
            return false;
        if (paymentRefund.Status == PaymentRefundStatus.Pending)
        {
            paymentRefund.MarkSucceeded(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        return await sender.Send(
            new ProcessItemRefundReversalCommand(request.OrganizationId, itemRefund.Id),
            cancellationToken
        );
    }
}
