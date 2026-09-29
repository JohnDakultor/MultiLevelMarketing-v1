using System.Collections.Concurrent;
using System.Reflection;
using modular_mlm.Application.Common.Features;

namespace modular_mlm.Application.Common.Behaviours;

public sealed class OrganizationFeatureGateBehaviour<TRequest, TResponse>(
    IOrganizationFeatureGate featureGate
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> OrganizationProperties = new();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var feature = OrganizationFeatureRequirementResolver.Resolve(request.GetType());
        if (feature.HasValue && TryGetOrganizationId(request, out var organizationId))
            await featureGate.EnsureEnabledAsync(organizationId, feature.Value, cancellationToken);

        return await next();
    }

    private static bool TryGetOrganizationId(TRequest request, out Guid organizationId)
    {
        var property = OrganizationProperties.GetOrAdd(
            request.GetType(),
            type => type.GetProperty("OrganizationId", BindingFlags.Public | BindingFlags.Instance)
        );
        if (property?.PropertyType == typeof(Guid) && property.GetValue(request) is Guid value)
        {
            organizationId = value;
            return value != Guid.Empty;
        }
        organizationId = Guid.Empty;
        return false;
    }
}
