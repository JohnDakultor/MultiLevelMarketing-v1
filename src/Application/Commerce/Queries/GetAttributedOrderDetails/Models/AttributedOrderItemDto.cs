using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails.Models;

public sealed record AttributedOrderItemDto(
    Guid OrderItemId,
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    int Quantity,
    FulfillmentStatus FulfillmentStatus,
    decimal CommissionableAmount,
    decimal BusinessVolume
);
