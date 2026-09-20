namespace modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary.Models;

public sealed record AgentProductSalesSummaryDto(
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    decimal QuantitySold,
    string Currency,
    decimal GrossAttributedSales,
    decimal CommissionableSales,
    decimal BusinessVolume,
    decimal CommissionAmount
);
