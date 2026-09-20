namespace modular_mlm.Application.Catalog.Queries.GetProducts.Models;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? ImageUrl,
    decimal Price,
    decimal BusinessVolume
);
