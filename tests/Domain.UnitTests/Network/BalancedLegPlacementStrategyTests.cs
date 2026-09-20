using modular_mlm.Domain.Network;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class BalancedLegPlacementStrategyTests
{
    private readonly BalancedLegPlacementStrategy _strategy = new();

    [Test]
    public void SelectsSmallerOpenLeg()
    {
        var parentId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(parentId, depth: 0, order: 0, leftSize: 5, rightSize: 2),
        };

        var result = _strategy.SelectSlot(candidates);

        result.ParentAgentId.ShouldBe(parentId);
        result.Side.ShouldBe(PlacementSide.Right);
    }

    [Test]
    public void UsesPreferredSideWhenLegSizesAreEqual()
    {
        var candidate = Candidate(Guid.NewGuid(), depth: 0, order: 0, leftSize: 3, rightSize: 3);

        var result = _strategy.SelectSlot(new[] { candidate }, PlacementSide.Right);

        result.Side.ShouldBe(PlacementSide.Right);
    }

    [Test]
    public void UsesOnlyAvailableSideRegardlessOfLegSize()
    {
        var candidate = Candidate(
            Guid.NewGuid(),
            depth: 0,
            order: 0,
            leftSize: 10,
            rightSize: 0,
            rightOccupied: true
        );

        var result = _strategy.SelectSlot(new[] { candidate }, PlacementSide.Right);

        result.Side.ShouldBe(PlacementSide.Left);
    }

    [Test]
    public void PrefersShallowerAvailableCandidate()
    {
        var shallowId = Guid.NewGuid();
        var deepId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(deepId, depth: 2, order: 0, leftSize: 0, rightSize: 0),
            Candidate(shallowId, depth: 1, order: 1, leftSize: 8, rightSize: 7),
        };

        var result = _strategy.SelectSlot(candidates);

        result.ParentAgentId.ShouldBe(shallowId);
    }

    [Test]
    public void UsesTraversalOrderAsDeterministicTieBreaker()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(secondId, depth: 1, order: 2, leftSize: 1, rightSize: 1),
            Candidate(firstId, depth: 1, order: 1, leftSize: 1, rightSize: 1),
        };

        var result = _strategy.SelectSlot(candidates);

        result.ParentAgentId.ShouldBe(firstId);
    }

    [Test]
    public void ThrowsWhenEveryCandidateIsFull()
    {
        var candidates = new[]
        {
            Candidate(
                Guid.NewGuid(),
                depth: 0,
                order: 0,
                leftSize: 1,
                rightSize: 1,
                leftOccupied: true,
                rightOccupied: true
            ),
        };

        Should.Throw<InvalidOperationException>(() => _strategy.SelectSlot(candidates));
    }

    private static PlacementCandidate Candidate(
        Guid id,
        int depth,
        long order,
        int leftSize,
        int rightSize,
        bool leftOccupied = false,
        bool rightOccupied = false
    ) => new(id, depth, order, leftOccupied, rightOccupied, leftSize, rightSize);
}
