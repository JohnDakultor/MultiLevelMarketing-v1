using modular_mlm.Application.Commerce.Commands.RequestItemRefund;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;
using modular_mlm.Domain.Services;

namespace modular_mlm.Application.Commerce.Commands.RequestRefund;

public sealed class RequestRefundCommandHandler(
    IApplicationDbContext db,
    IUser currentUser,
    CustomerRefundEligibilityPolicy refundPolicy,
    TimeProvider clock,
    ISender sender
) : IRequestHandler<RequestRefundCommand, Guid>
{
    public async Task<Guid> Handle(
        RequestRefundCommand request,
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
            .Orders.AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.OrderId
                    && candidate.OrganizationId == request.OrganizationId
                    && candidate.CustomerId == customerId.Value,
                cancellationToken
            );
        if (order is null)
            throw new KeyNotFoundException("Order was not found.");

        var item = order.Items.SingleOrDefault(candidate => candidate.Id == request.OrderItemId);
        if (item is null)
            throw new KeyNotFoundException("Order item was not found.");

        const string providerReason = "requested_by_customer";
        var normalizedReason = request.Reason.Trim();
        var pendingRequest = await (
            from itemRefund in db.OrderItemRefunds.AsNoTracking()
            join paymentRefund in db.PaymentRefunds.AsNoTracking()
                on itemRefund.PaymentRefundId equals paymentRefund.Id
            where
                itemRefund.OrganizationId == request.OrganizationId
                && itemRefund.OrderId == request.OrderId
                && itemRefund.OrderItemId == request.OrderItemId
                && paymentRefund.Status == PaymentRefundStatus.Pending
            select new
            {
                itemRefund.Id,
                itemRefund.Quantity,
                paymentRefund.Reason,
            }
        ).SingleOrDefaultAsync(cancellationToken);
        if (pendingRequest is not null)
        {
            if (
                pendingRequest.Quantity == request.Quantity
                && string.Equals(
                    pendingRequest.Reason,
                    providerReason,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return pendingRequest.Id;

            throw new InvalidOperationException(
                "This item already has a refund awaiting provider confirmation."
            );
        }

        var allocations = await (
            from itemRefund in db.OrderItemRefunds.AsNoTracking()
            join paymentRefund in db.PaymentRefunds.AsNoTracking()
                on itemRefund.PaymentRefundId equals paymentRefund.Id
            where
                itemRefund.OrganizationId == request.OrganizationId
                && itemRefund.OrderItemId == request.OrderItemId
                && paymentRefund.Status != PaymentRefundStatus.Failed
            select itemRefund.Quantity
        ).ToListAsync(cancellationToken);
        var returnWindowDays =
            await db
                .WalletSettings.AsNoTracking()
                .Where(settings => settings.OrganizationId == request.OrganizationId)
                .Select(settings => (int?)settings.ReturnWindowDays)
                .SingleOrDefaultAsync(cancellationToken)
            ?? 0;

        var decision = refundPolicy.Evaluate(
            new CustomerRefundEligibilityInput(
                order.Status,
                order.PaymentStatus,
                item.FulfillmentStatus,
                item.Quantity,
                allocations.Sum(),
                false,
                order.DeliveredAt,
                returnWindowDays,
                clock.GetUtcNow()
            )
        );
        if (!decision.IsEligible || request.Quantity > decision.MaximumQuantity)
            throw new InvalidOperationException(
                decision.IsEligible
                    ? "Refund quantity exceeds the remaining refundable quantity."
                    : decision.Explanation
            );

        return await sender.Send(
            new RequestItemRefundCommand(
                request.OrganizationId,
                request.OrderId,
                request.OrderItemId,
                request.Quantity,
                providerReason,
                normalizedReason
            ),
            cancellationToken
        );
    }
}
