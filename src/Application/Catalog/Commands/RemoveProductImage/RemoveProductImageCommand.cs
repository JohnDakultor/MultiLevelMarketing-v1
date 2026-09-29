namespace modular_mlm.Application.Catalog.Commands.RemoveProductImage;

public sealed record RemoveProductImageCommand(Guid OrganizationId, Guid ProductId)
    : IRequest,
        IOrganizationAdminRequest;
