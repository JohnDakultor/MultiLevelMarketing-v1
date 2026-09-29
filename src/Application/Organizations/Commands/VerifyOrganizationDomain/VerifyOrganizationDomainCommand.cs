namespace modular_mlm.Application.Organizations.Commands.VerifyOrganizationDomain;

public sealed record VerifyOrganizationDomainCommand(
    Guid OrganizationId,
    Guid OrganizationDomainId
) : IRequest, IOrganizationAdminRequest;
