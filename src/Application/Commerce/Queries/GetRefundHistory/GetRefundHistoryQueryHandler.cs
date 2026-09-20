using modular_mlm.Application.Commerce.Queries.GetRefundHistory.Models;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Commerce.Queries.GetRefundHistory;

public sealed class GetRefundHistoryQueryHandler(
    IApplicationDbContext db,
    IUser currentUser,
    IAdministratorAccountService administratorAccounts
) : IRequestHandler<GetRefundHistoryQuery, IReadOnlyList<RefundHistoryItemDto>>
{
    public async Task<IReadOnlyList<RefundHistoryItemDto>> Handle(
        GetRefundHistoryQuery request,
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

        var query =
            from itemRefund in db.OrderItemRefunds.AsNoTracking()
            join paymentRefund in db.PaymentRefunds.AsNoTracking()
                on itemRefund.PaymentRefundId equals paymentRefund.Id
            where itemRefund.OrganizationId == request.OrganizationId
            select new { itemRefund, paymentRefund };
        if (request.OrderId.HasValue)
            query = query.Where(row => row.itemRefund.OrderId == request.OrderId.Value);

        return await query
            .OrderByDescending(row => row.itemRefund.RequestedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(row => new RefundHistoryItemDto(
                row.itemRefund.Id,
                row.itemRefund.OrderId,
                row.itemRefund.OrderItemId,
                row.itemRefund.Quantity,
                row.itemRefund.RefundAmount,
                row.paymentRefund.Currency,
                row.paymentRefund.Reason,
                row.itemRefund.Status,
                row.paymentRefund.Status,
                row.paymentRefund.ProviderRefundId,
                row.paymentRefund.RequestedAt,
                row.paymentRefund.CompletedAt,
                row.itemRefund.CompletedAt,
                row.itemRefund.ReversalFailure ?? row.paymentRefund.FailureMessage
            ))
            .ToListAsync(cancellationToken);
    }
}
