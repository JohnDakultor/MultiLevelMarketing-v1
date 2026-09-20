using System.Text.Json;
using modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails.Models;
using modular_mlm.Application.Commerce.Queries.GetOrderDetails.Models;
using modular_mlm.Application.Commerce.Queries.GetRefundHistory.Models;

namespace modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails;

public sealed class GetAdminOrderDetailsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminOrderDetailsQuery, AdminOrderDetailsDto?>
{
    private static readonly JsonSerializerOptions AddressJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AdminOrderDetailsDto?> Handle(
        GetAdminOrderDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        var order = await (
            from candidate in db.Orders.AsNoTracking().Include(candidate => candidate.Items)
            join customer in db.CustomerProfiles.AsNoTracking()
                on candidate.CustomerId equals customer.Id
            where
                candidate.Id == request.OrderId
                && candidate.OrganizationId == request.OrganizationId
                && customer.OrganizationId == request.OrganizationId
            select new { Order = candidate, CustomerName = customer.DisplayName }
        ).SingleOrDefaultAsync(cancellationToken);

        if (order is null)
            return null;

        var payments = await db
            .Payments.AsNoTracking()
            .Where(payment =>
                payment.OrganizationId == request.OrganizationId
                && payment.OrderId == request.OrderId
            )
            .OrderByDescending(payment => payment.Created)
            .ToListAsync(cancellationToken);
        var paymentIds = payments.Select(payment => payment.Id).ToArray();
        var refunds = await db
            .PaymentRefunds.AsNoTracking()
            .Where(refund =>
                refund.OrganizationId == request.OrganizationId
                && refund.OrderId == request.OrderId
                && paymentIds.Contains(refund.PaymentId)
            )
            .OrderByDescending(refund => refund.RequestedAt)
            .ToListAsync(cancellationToken);
        var itemRefunds = await (
            from itemRefund in db.OrderItemRefunds.AsNoTracking()
            join paymentRefund in db.PaymentRefunds.AsNoTracking()
                on itemRefund.PaymentRefundId equals paymentRefund.Id
            where
                itemRefund.OrganizationId == request.OrganizationId
                && itemRefund.OrderId == request.OrderId
                && paymentRefund.OrganizationId == request.OrganizationId
                && paymentRefund.OrderId == request.OrderId
            orderby itemRefund.RequestedAt descending
            select new RefundHistoryItemDto(
                itemRefund.Id,
                itemRefund.OrderId,
                itemRefund.OrderItemId,
                itemRefund.Quantity,
                itemRefund.RefundAmount,
                paymentRefund.Currency,
                paymentRefund.Reason,
                itemRefund.Status,
                paymentRefund.Status,
                paymentRefund.ProviderRefundId,
                paymentRefund.RequestedAt,
                paymentRefund.CompletedAt,
                itemRefund.CompletedAt,
                itemRefund.ReversalFailure ?? paymentRefund.FailureMessage
            )
        ).ToListAsync(cancellationToken);

        return new AdminOrderDetailsDto(
            order.Order.Id,
            order.Order.OrderNumber,
            order.Order.CustomerId,
            order.CustomerName,
            order.Order.Status,
            order.Order.PaymentStatus,
            order.Order.Currency,
            order.Order.Subtotal,
            order.Order.DiscountTotal,
            order.Order.ShippingTotal,
            order.Order.TaxTotal,
            order.Order.GrandTotal,
            DeserializeAddress(order.Order.ShippingAddressJson, "shipping"),
            DeserializeAddress(order.Order.BillingAddressJson, "billing"),
            order.Order.Created,
            order.Order.PaidAt,
            order.Order.DeliveredAt,
            order
                .Order.Items.OrderBy(item => item.ProductNameSnapshot)
                .Select(item => new AdminOrderItemDto(
                    item.Id,
                    item.ProductId,
                    item.ProductVariantId,
                    item.ProductNameSnapshot,
                    item.SkuSnapshot,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal,
                    item.BusinessVolume,
                    item.FulfillmentStatus
                ))
                .ToList(),
            payments
                .Select(payment => new AdminOrderPaymentDto(
                    payment.Id,
                    payment.Provider,
                    payment.ProviderCheckoutSessionId,
                    payment.ProviderPaymentId,
                    payment.Amount,
                    payment.RefundedAmount,
                    payment.Currency,
                    payment.Status,
                    payment.Created,
                    payment.PaidAt,
                    payment.FailureCode,
                    payment.FailureMessage,
                    refunds
                        .Where(refund => refund.PaymentId == payment.Id)
                        .Select(refund => new AdminPaymentRefundDto(
                            refund.Id,
                            refund.Amount,
                            refund.Currency,
                            refund.Reason,
                            refund.ProviderRefundId,
                            refund.Status,
                            refund.RequestedAt,
                            refund.CompletedAt,
                            refund.FailureMessage
                        ))
                        .ToList()
                ))
                .ToList(),
            itemRefunds
        );
    }

    private static AddressDto DeserializeAddress(string json, string addressType)
    {
        try
        {
            return JsonSerializer.Deserialize<AddressDto>(json, AddressJsonOptions)
                ?? throw new JsonException("Address was empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"The stored {addressType} address snapshot is invalid.",
                exception
            );
        }
    }
}
