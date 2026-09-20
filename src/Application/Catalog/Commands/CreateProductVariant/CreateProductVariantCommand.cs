namespace modular_mlm.Application.Catalog.Commands.CreateProductVariant;

public sealed record CreateProductVariantCommand(
    Guid OrganizationId,
    Guid ProductId,
    string Sku,
    decimal Price,
    decimal BusinessVolume,
    int InitialOnHandQuantity,
    bool StockKeepingEnabled
) : IRequest<Guid>, IOrganizationAdminRequest;
