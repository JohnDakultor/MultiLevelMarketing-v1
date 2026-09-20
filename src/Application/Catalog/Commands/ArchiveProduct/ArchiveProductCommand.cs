namespace modular_mlm.Application.Catalog.Commands.ArchiveProduct;

public sealed record ArchiveProductCommand(Guid OrganizationId, Guid ProductId)
    : IRequest,
        IOrganizationAdminRequest;
