using modular_mlm.Domain.Network;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class BreadthFirstPlacementStrategyTests
{
    private readonly BreadthFirstPlacementStrategy _strategy = new();

    [Test]
    public void SelectsShallowestAvailableCandidate()
    {
        var shallowId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), depth: 2, order: 0),
            Candidate(shallowId, depth: 1, order: 5),
        };

        var result = _strategy.SelectSlot(candidates);

        result.ParentAgentId.ShouldBe(shallowId);
    }

    [Test]
    public void SelectsLowestTraversalOrderAtSameDepth()
    {
        var firstId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), depth: 1, order: 2),
            Candidate(firstId, depth: 1, order: 1),
        };

        var result = _strategy.SelectSlot(candidates);

        result.ParentAgentId.ShouldBe(firstId);
    }

    [Test]
    public void SelectsLeftSlotBeforeRightSlot()
    {
        var result = _strategy.SelectSlot(new[] { Candidate(Guid.NewGuid(), depth: 0, order: 0) });

        result.Side.ShouldBe(PlacementSide.Left);
    }

    [Test]
    public void SelectsRightWhenLeftIsOccupied()
    {
        var result = _strategy.SelectSlot(
            new[] { Candidate(Guid.NewGuid(), depth: 0, order: 0, leftOccupied: true) }
        );

        result.Side.ShouldBe(PlacementSide.Right);
    }

    [Test]
    public void SkipsFullCandidate()
    {
        var availableId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), depth: 0, order: 0, leftOccupied: true, rightOccupied: true),
            Candidate(availableId, depth: 1, order: 1),
        };

        var result = _strategy.SelectSlot(candidates);

        result.ParentAgentId.ShouldBe(availableId);
    }

    [Test]
    public void IgnoresPreferredSideToPreserveLeftToRightOrdering()
    {
        var result = _strategy.SelectSlot(
            new[] { Candidate(Guid.NewGuid(), depth: 0, order: 0) },
            PlacementSide.Right
        );

        result.Side.ShouldBe(PlacementSide.Left);
    }

    [Test]
    public void ThrowsWhenEveryCandidateIsFull()
    {
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), depth: 0, order: 0, leftOccupied: true, rightOccupied: true),
        };

        Should.Throw<InvalidOperationException>(() => _strategy.SelectSlot(candidates));
    }

    private static PlacementCandidate Candidate(
        Guid id,
        int depth,
        long order,
        bool leftOccupied = false,
        bool rightOccupied = false
    ) => new(id, depth, order, leftOccupied, rightOccupied);
}
