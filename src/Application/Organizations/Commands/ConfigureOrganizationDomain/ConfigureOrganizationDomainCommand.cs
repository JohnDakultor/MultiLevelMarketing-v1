namespace modular_mlm.Application.Organizations.Commands.ConfigureOrganizationDomain;

public sealed record ConfigureOrganizationDomainCommand(
    Guid OrganizationId,
    string HostName,
    bool MakePrimary
) : IRequest<Guid>, IOrganizationAdminRequest;
