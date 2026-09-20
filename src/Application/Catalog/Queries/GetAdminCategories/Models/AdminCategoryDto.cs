namespace modular_mlm.Application.Catalog.Queries.GetAdminCategories.Models;

public sealed record AdminCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    int ProductCount,
    DateTimeOffset Created,
    DateTimeOffset LastModified
);
