namespace modular_mlm.Application.Catalog.Commands.CreateProduct;

public sealed record CreateProductCommand(
    Guid OrganizationId,
    Guid CategoryId,
    string Name,
    string Slug,
    string Description,
    string Sku,
    decimal Price,
    decimal BusinessVolume,
    int StockQuantity
) : IRequest<Guid>, IOrganizationAdminRequest;
