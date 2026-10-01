using modular_mlm.Application.Organizations.Queries.GetOrganizationProvisioning.Models;

namespace modular_mlm.Application.Organizations.Queries.GetOrganizationProvisioning;

public sealed record GetOrganizationProvisioningQuery(string Slug)
    : IRequest<OrganizationProvisioningDto?>,
        IAuthorizeRequest
{
    public Task<bool> IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) => authorizationService.CanProvisionOrganizationAsync(cancellationToken);
}
