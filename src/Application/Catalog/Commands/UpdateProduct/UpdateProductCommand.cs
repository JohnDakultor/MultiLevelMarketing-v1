namespace modular_mlm.Application.Catalog.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid OrganizationId,
    Guid ProductId,
    Guid CategoryId,
    string Name,
    string Description
) : IRequest, IOrganizationAdminRequest;
