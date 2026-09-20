namespace modular_mlm.Application.Catalog.Commands.ArchiveProductVariant;

public sealed record ArchiveProductVariantCommand(
    Guid OrganizationId,
    Guid ProductId,
    Guid ProductVariantId,
    string Reason,
    int ExpectedVersion
) : IRequest<Guid>, IOrganizationAdminRequest;
