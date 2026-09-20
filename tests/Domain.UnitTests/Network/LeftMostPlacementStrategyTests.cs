using modular_mlm.Domain.Network;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class LeftMostPlacementStrategyTests
{
    private readonly LeftMostPlacementStrategy _strategy = new();

    [Test]
    public void SelectsLowestTraversalOrder()
    {
        var expectedId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), depth: 1, order: 5),
            Candidate(expectedId, depth: 3, order: 1),
        };

        _strategy.SelectSlot(candidates).ParentAgentId.ShouldBe(expectedId);
    }

    [Test]
    public void PrefersLeftSlot()
    {
        _strategy
            .SelectSlot(new[] { Candidate(Guid.NewGuid(), 0, 0) })
            .Side.ShouldBe(PlacementSide.Left);
    }

    [Test]
    public void UsesRightSlotWhenLeftIsOccupied()
    {
        _strategy
            .SelectSlot(new[] { Candidate(Guid.NewGuid(), 0, 0, leftOccupied: true) })
            .Side.ShouldBe(PlacementSide.Right);
    }

    [Test]
    public void SkipsFullLeftMostCandidate()
    {
        var expectedId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), 0, 0, true, true),
            Candidate(expectedId, 1, 1),
        };

        _strategy.SelectSlot(candidates).ParentAgentId.ShouldBe(expectedId);
    }

    [Test]
    public void ThrowsWhenNoSlotIsAvailable()
    {
        Should.Throw<InvalidOperationException>(() =>
            _strategy.SelectSlot(new[] { Candidate(Guid.NewGuid(), 0, 0, true, true) })
        );
    }

    private static PlacementCandidate Candidate(
        Guid id,
        int depth,
        long order,
        bool leftOccupied = false,
        bool rightOccupied = false
    ) => new(id, depth, order, leftOccupied, rightOccupied);
}
