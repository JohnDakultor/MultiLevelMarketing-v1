using modular_mlm.Application.Common.Services;

namespace modular_mlm.Application.Common.Behaviours;

public sealed class CustomerContextProvisioningBehaviour<TRequest, TResponse>(
    CustomerContextProvisioner provisioner
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (request is ICustomerContextRequest customerRequest)
            await provisioner.EnsureProvisionedAndMergeCartAsync(
                customerRequest.OrganizationId,
                cancellationToken
            );

        return await next();
    }
}
