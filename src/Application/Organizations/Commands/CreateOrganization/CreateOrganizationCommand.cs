namespace modular_mlm.Application.Organizations.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    string Slug,
    string CurrencyCode,
    string TimeZone = "UTC",
    string Locale = "en"
) : IRequest<Guid>, IAuthorizeRequest
{
    public Task<bool> IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) => authorizationService.CanProvisionOrganizationAsync(cancellationToken);
}
