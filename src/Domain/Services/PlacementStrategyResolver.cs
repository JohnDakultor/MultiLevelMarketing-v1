using modular_mlm.Domain.Network;

namespace modular_mlm.Domain.Services;

public sealed class PlacementStrategyResolver(IEnumerable<IPlacementStrategy> strategies)
{
    private readonly IReadOnlyDictionary<PlacementStrategyType, IPlacementStrategy> _strategies =
        strategies.ToDictionary(GetStrategyType);

    public IPlacementStrategy Resolve(PlacementStrategyType strategyType) =>
        _strategies.TryGetValue(strategyType, out var strategy)
            ? strategy
            : throw new InvalidOperationException(
                $"Placement strategy '{strategyType}' is not registered."
            );

    private static PlacementStrategyType GetStrategyType(IPlacementStrategy strategy) =>
        strategy.Name switch
        {
            BreadthFirstPlacementStrategy.StrategyName => PlacementStrategyType.BreadthFirst,
            PreferredLegPlacementStrategy.StrategyName => PlacementStrategyType.PreferredLeg,
            LeftMostPlacementStrategy.StrategyName => PlacementStrategyType.LeftMost,
            RightMostPlacementStrategy.StrategyName => PlacementStrategyType.RightMost,
            BalancedLegPlacementStrategy.StrategyName => PlacementStrategyType.BalancedLeg,
            _ => throw new InvalidOperationException(
                $"Unknown placement strategy registration '{strategy.Name}'."
            ),
        };
}
