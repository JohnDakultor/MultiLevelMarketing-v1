namespace modular_mlm.Application.Catalog.Commands.PublishProduct;

public sealed record PublishProductCommand(Guid OrganizationId, Guid ProductId)
    : IRequest,
        IOrganizationAdminRequest;
