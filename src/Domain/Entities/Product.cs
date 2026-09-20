using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Catalog;

public sealed class Product : OrganizationEntity
{
    private readonly List<ProductVariant> _variants = [];

    private Product() { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProductStatus Status { get; private set; }
    public Guid CategoryId { get; private set; }
    public string? Brand { get; private set; }
    public string? DefaultImageUrl { get; private set; }
    public Guid? CommissionProfileId { get; private set; }
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public static Product Create(
        Guid organizationId,
        Guid categoryId,
        string name,
        string slug,
        string description
    )
    {
        if (organizationId == Guid.Empty || categoryId == Guid.Empty)
            throw new DomainInvariantException("Organization and category are required.");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
            throw new DomainInvariantException("Product name and slug are required.");
        return new Product
        {
            OrganizationId = organizationId,
            CategoryId = categoryId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description.Trim(),
            Status = ProductStatus.Draft,
        };
    }

    public ProductVariant AddVariant(
        string sku,
        decimal price,
        decimal businessVolume,
        int stockQuantity,
        bool stockKeepingEnabled = true
    )
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainInvariantException("SKU is required.");
        if (_variants.Any(x => x.Sku.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainInvariantException("SKU must be unique within a product.");
        var variant = ProductVariant.Create(
            Id,
            sku,
            price,
            businessVolume,
            stockQuantity,
            stockKeepingEnabled: stockKeepingEnabled
        );
        _variants.Add(variant);
        return variant;
    }

    public void UpdateDetails(string name, string description, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name) || categoryId == Guid.Empty)
            throw new DomainInvariantException("Name and category are required.");
        Name = name.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
    }

    public void AssignCommissionProfile(Guid? profileId) => CommissionProfileId = profileId;

    public void Publish()
    {
        if (_variants.Count == 0)
            throw new DomainInvariantException(
                "A product needs at least one variant before publication."
            );
        Status = ProductStatus.Active;
    }

    public void Archive() => Status = ProductStatus.Archived;
}
