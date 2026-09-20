namespace modular_mlm.Application.Organizations.Commands.RemoveOrganizationDomain;

public sealed record RemoveOrganizationDomainCommand(Guid OrganizationId, Guid OrganizationDomainId)
    : IRequest,
        IOrganizationAdminRequest;
