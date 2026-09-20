using System.Reflection;
using Domain.Services;
using Microsoft.Extensions.Hosting;
using modular_mlm.Application.Common.Behaviours;
using modular_mlm.Application.Common.Services;
using modular_mlm.Domain.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddSingleton<IPlacementStrategy, BreadthFirstPlacementStrategy>();
        builder.Services.AddSingleton<IPlacementStrategy, PreferredLegPlacementStrategy>();
        builder.Services.AddSingleton<IPlacementStrategy, LeftMostPlacementStrategy>();
        builder.Services.AddSingleton<IPlacementStrategy, RightMostPlacementStrategy>();
        builder.Services.AddSingleton<IPlacementStrategy, BalancedLegPlacementStrategy>();
        builder.Services.AddSingleton<PlacementStrategyResolver>();
        builder.Services.AddSingleton<PlacementMoveEligibilityPolicy>();
        builder.Services.AddSingleton<OrderCancellationPolicy>();
        builder.Services.AddSingleton<CustomerRefundEligibilityPolicy>();
        builder.Services.AddSingleton<PayoutEligibilityEvaluator>();
        builder.Services.AddScoped<CustomerContextProvisioner>();

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenRequestPreProcessor(typeof(LoggingBehaviour<>));
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(CustomerContextProvisioningBehaviour<,>));
            cfg.AddOpenBehavior(typeof(DynamicAuthorizationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(QueryCachingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
        });
    }
}
