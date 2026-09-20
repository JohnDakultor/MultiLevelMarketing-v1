using System.Text.Json;
using modular_mlm.Application.Commerce.Queries.GetOrderDetails.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;
using modular_mlm.Domain.Services;

namespace modular_mlm.Application.Commerce.Queries.GetOrderDetails;

public sealed class GetOrderDetailsQueryHandler(
    IApplicationDbContext db,
    IUser currentUser,
    OrderCancellationPolicy cancellationPolicy,
    CustomerRefundEligibilityPolicy refundPolicy,
    TimeProvider clock
) : IRequestHandler<GetOrderDetailsQuery, OrderDetailsDto>
{
    private static readonly JsonSerializerOptions AddressJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<OrderDetailsDto> Handle(
        GetOrderDetailsQuery request,
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

        var returnWindowDays =
            await db
                .WalletSettings.AsNoTracking()
                .Where(settings => settings.OrganizationId == request.OrganizationId)
                .Select(settings => (int?)settings.ReturnWindowDays)
                .SingleOrDefaultAsync(cancellationToken)
            ?? 0;

        var refundAllocations = await (
            from itemRefund in db.OrderItemRefunds.AsNoTracking()
            join paymentRefund in db.PaymentRefunds.AsNoTracking()
                on itemRefund.PaymentRefundId equals paymentRefund.Id
            where
                itemRefund.OrganizationId == request.OrganizationId
                && itemRefund.OrderId == order.Id
            select new
            {
                itemRefund.OrderItemId,
                itemRefund.Quantity,
                ItemRefundStatus = itemRefund.Status,
                PaymentRefundStatus = paymentRefund.Status,
            }
        ).ToListAsync(cancellationToken);

        var cancellationDecision = cancellationPolicy.Evaluate(
            order.Status,
            order.PaymentStatus,
            order.Items.Select(item => item.FulfillmentStatus)
        );
        var now = clock.GetUtcNow();
        var items = order
            .Items.OrderBy(item => item.ProductNameSnapshot)
            .ThenBy(item => item.SkuSnapshot)
            .Select(item =>
            {
                var itemAllocations = refundAllocations
                    .Where(allocation => allocation.OrderItemId == item.Id)
                    .ToList();
                var allocatedQuantity = itemAllocations
                    .Where(allocation =>
                        allocation.PaymentRefundStatus != PaymentRefundStatus.Failed
                    )
                    .Sum(allocation => allocation.Quantity);
                var refundedQuantity = itemAllocations
                    .Where(allocation =>
                        allocation.PaymentRefundStatus == PaymentRefundStatus.Succeeded
                        && allocation.ItemRefundStatus == OrderItemRefundStatus.Reversed
                    )
                    .Sum(allocation => allocation.Quantity);
                var refundDecision = refundPolicy.Evaluate(
                    new CustomerRefundEligibilityInput(
                        order.Status,
                        order.PaymentStatus,
                        item.FulfillmentStatus,
                        item.Quantity,
                        allocatedQuantity,
                        itemAllocations.Any(allocation =>
                            allocation.PaymentRefundStatus == PaymentRefundStatus.Pending
                        ),
                        order.DeliveredAt,
                        returnWindowDays,
                        now
                    )
                );

                return new OrderItemDetailsDto(
                    item.Id,
                    item.ProductId,
                    item.ProductVariantId,
                    item.ProductNameSnapshot,
                    item.SkuSnapshot,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal,
                    item.BusinessVolume,
                    item.FulfillmentStatus,
                    refundedQuantity,
                    Math.Max(0m, item.Quantity - allocatedQuantity),
                    refundDecision.IsEligible,
                    refundDecision.IsEligible ? null : refundDecision.Explanation
                );
            })
            .ToList();

        return new OrderDetailsDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.PaymentStatus,
            order.Currency,
            order.Subtotal,
            order.DiscountTotal,
            order.ShippingTotal,
            order.TaxTotal,
            order.GrandTotal,
            DeserializeAddress(order.ShippingAddressJson, "shipping"),
            DeserializeAddress(order.BillingAddressJson, "billing"),
            order.Created,
            order.PaidAt,
            order.DeliveredAt,
            cancellationDecision.IsAllowed,
            cancellationDecision.IsAllowed ? null : cancellationDecision.Explanation,
            items
        );
    }

    private static AddressDto DeserializeAddress(string json, string addressType)
    {
        try
        {
            var address = JsonSerializer.Deserialize<AddressDto>(json, AddressJsonOptions);
            if (
                address is null
                || string.IsNullOrWhiteSpace(address.RecipientName)
                || string.IsNullOrWhiteSpace(address.AddressLine1)
                || string.IsNullOrWhiteSpace(address.CityOrMunicipality)
                || string.IsNullOrWhiteSpace(address.Province)
                || string.IsNullOrWhiteSpace(address.PostalCode)
                || string.IsNullOrWhiteSpace(address.CountryCode)
            )
                throw new JsonException("Required address fields are missing.");

            return address;
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
