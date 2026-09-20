using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Common.Behaviours;

public sealed class DynamicAuthorizationBehaviour<TRequest, TResponse>(
    IApplicationAuthorizationService authorizationService
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (request is IAuthorizeRequest authorizeRequest)
        {
            var isAuthorized = await authorizeRequest.IsAuthorizedAsync(
                authorizationService,
                cancellationToken
            );

            if (!isAuthorized)
                throw new ForbiddenAccessException();
        }

        return await next();
    }
}
