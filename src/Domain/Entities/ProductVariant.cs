using System.Text.Json;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Catalog;

public sealed class ProductVariant : BaseAuditableEntity
{
    private ProductVariant() { }

    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public decimal BusinessVolume { get; private set; }
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity =>
        StockKeepingEnabled ? StockQuantity - ReservedQuantity : int.MaxValue;
    public bool StockKeepingEnabled { get; private set; }
    public decimal? Weight { get; private set; }
    public string AttributesJson { get; private set; } = "{}";

    public int Version { get; private set; }

    public ProductStatus Status { get; private set; } = ProductStatus.Draft;

    internal static ProductVariant Create(
        Guid productId,
        string sku,
        decimal price,
        decimal businessVolume,
        int stockQuantity,
        ProductStatus status = ProductStatus.Draft,
        int version = 1,
        bool stockKeepingEnabled = true
    )
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainInvariantException("SKU is required.");
        if (price < 0 || businessVolume < 0 || stockQuantity < 0)
            throw new DomainInvariantException("Price, BV, and stock cannot be negative.");
        if (!stockKeepingEnabled && stockQuantity != 0)
            throw new DomainInvariantException("Untracked variants must have zero initial stock.");
        return new ProductVariant
        {
            ProductId = productId,
            Sku = sku.Trim().ToUpperInvariant(),
            Price = price,
            BusinessVolume = businessVolume,
            StockQuantity = stockQuantity,
            StockKeepingEnabled = stockKeepingEnabled,
            Status = status,
            Version = version,
        };
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
            throw new DomainInvariantException("Price cannot be negative.");
        Price = price;
    }

    public void ChangeBusinessVolume(decimal volume)
    {
        if (volume < 0)
            throw new DomainInvariantException("BV cannot be negative.");
        BusinessVolume = volume;
    }

    public void UpdateDetails(
        decimal price,
        decimal businessVolume,
        decimal? weight,
        string attributesJson,
        bool stockKeepingEnabled
    )
    {
        if (price < 0 || businessVolume < 0 || weight < 0)
            throw new DomainInvariantException("Variant values cannot be negative.");
        attributesJson = string.IsNullOrWhiteSpace(attributesJson) ? "{}" : attributesJson.Trim();
        try
        {
            using var _ = JsonDocument.Parse(attributesJson);
        }
        catch (JsonException)
        {
            throw new DomainInvariantException("Variant attributes must contain valid JSON.");
        }
        if (!stockKeepingEnabled && (StockQuantity != 0 || ReservedQuantity != 0))
            throw new DomainInvariantException(
                "Stock keeping cannot be disabled while stock or reservations exist."
            );

        Price = price;
        BusinessVolume = businessVolume;
        Weight = weight;
        AttributesJson = attributesJson;
        StockKeepingEnabled = stockKeepingEnabled;
        Version++;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainInvariantException("Stock addition must be positive.");
        checked
        {
            StockQuantity += quantity;
        }
        Version++;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainInvariantException("Quantity must be positive.");
        if (StockKeepingEnabled && AvailableQuantity < quantity)
            throw new DomainInvariantException($"Insufficient stock for SKU {Sku}.");
        if (StockKeepingEnabled)
        {
            ReservedQuantity = checked(ReservedQuantity + quantity);
            Version++;
        }
    }

    public void ReleaseReservedStock(int quantity)
    {
        if (quantity <= 0 || quantity > ReservedQuantity)
            throw new DomainInvariantException("Released quantity exceeds reserved stock.");
        ReservedQuantity -= quantity;
        Version++;
    }

    public void FinalizeReservedStock(int quantity)
    {
        if (quantity <= 0 || quantity > ReservedQuantity || quantity > StockQuantity)
            throw new DomainInvariantException("Finalized quantity exceeds reserved stock.");
        ReservedQuantity -= quantity;
        StockQuantity -= quantity;
        Version++;
    }

    public void AdjustOnHand(int quantityDelta)
    {
        if (!StockKeepingEnabled)
            throw new DomainInvariantException(
                "Inventory is not tracked for this product variant."
            );
        if (quantityDelta == 0)
            throw new DomainInvariantException("Inventory adjustment must be non-zero.");

        int adjustedQuantity;
        try
        {
            adjustedQuantity = checked(StockQuantity + quantityDelta);
        }
        catch (OverflowException)
        {
            throw new DomainInvariantException(
                "Inventory adjustment exceeds the supported quantity range."
            );
        }

        if (adjustedQuantity < ReservedQuantity)
            throw new DomainInvariantException(
                "Inventory adjustment cannot reduce on-hand stock below reserved stock."
            );

        StockQuantity = adjustedQuantity;
        Version++;
    }

    public void Archive()
    {
        if (Status == ProductStatus.Archived)
            return;
        if (ReservedQuantity > 0)
            throw new DomainInvariantException("A variant with reserved stock cannot be archived.");
        Status = ProductStatus.Archived;
        Version++;
    }
}
