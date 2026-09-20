using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Catalog;

public sealed class Category : OrganizationEntity
{
    private Category() { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static Category Create(Guid organizationId, string name, string slug)
    {
        if (organizationId == Guid.Empty)
            throw new DomainInvariantException("Organization is required.");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
            throw new DomainInvariantException("Category name and slug are required.");
        return new Category
        {
            OrganizationId = organizationId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            IsActive = true,
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainInvariantException("Category name is required.");
        Name = name.Trim();
    }

    public void Activate() => IsActive = true;

    public void Archive() => IsActive = false;
}
