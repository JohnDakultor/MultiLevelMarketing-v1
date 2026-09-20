namespace modular_mlm.Application.Catalog.Queries.GetAdminCategories.Models;

public sealed record AdminCategoriesPageDto(
    IReadOnlyList<AdminCategoryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
