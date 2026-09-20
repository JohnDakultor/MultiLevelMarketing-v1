namespace modular_mlm.Application.Common.Interfaces;

public interface IAuthorizeRequest
{
    Task<bool> IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    );
}
