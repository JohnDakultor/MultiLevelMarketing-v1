using modular_mlm.Domain.Network;
using modular_mlm.Domain.Services;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class PlacementStrategyResolverTests
{
    private readonly PlacementStrategyResolver _resolver = new(
        new IPlacementStrategy[]
        {
            new BreadthFirstPlacementStrategy(),
            new PreferredLegPlacementStrategy(),
            new LeftMostPlacementStrategy(),
            new RightMostPlacementStrategy(),
            new BalancedLegPlacementStrategy(),
        }
    );

    [TestCase(PlacementStrategyType.BreadthFirst, typeof(BreadthFirstPlacementStrategy))]
    [TestCase(PlacementStrategyType.PreferredLeg, typeof(PreferredLegPlacementStrategy))]
    [TestCase(PlacementStrategyType.LeftMost, typeof(LeftMostPlacementStrategy))]
    [TestCase(PlacementStrategyType.RightMost, typeof(RightMostPlacementStrategy))]
    [TestCase(PlacementStrategyType.BalancedLeg, typeof(BalancedLegPlacementStrategy))]
    public void ResolvesConfiguredStrategy(PlacementStrategyType type, Type expectedType)
    {
        _resolver.Resolve(type).GetType().ShouldBe(expectedType);
    }
}
