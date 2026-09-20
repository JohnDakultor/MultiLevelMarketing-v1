using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Data.Repositories;
using modular_mlm.Infrastructure.Identity;

namespace Microsoft.Extensions.DependencyInjection;

public static class AgentOperationsServiceCollectionExtensions
{
    public static IServiceCollection AddAgentOperationsInfrastructure(
        this IServiceCollection services
    )
    {
        services.AddScoped<AgentPlacementRepository>();
        services.AddScoped<IAgentPlacementRepository>(provider =>
            provider.GetRequiredService<AgentPlacementRepository>()
        );
        services.AddScoped<IAgentPlacementMutationRepository, AgentPlacementMutationRepository>();
        services.AddScoped<IAgentIdentityReader, AgentIdentityReader>();
        return services;
    }
}
