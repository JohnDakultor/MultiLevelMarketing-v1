using modular_mlm.Domain.Network;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class PreferredLegPlacementStrategyTests
{
    private readonly PreferredLegPlacementStrategy _strategy = new();

    [Test]
    public void SelectsPreferredSideWhenAvailable()
    {
        var id = Guid.NewGuid();
        var result = _strategy.SelectSlot(
            new[] { Candidate(id, depth: 0, order: 0) },
            PlacementSide.Right
        );

        result.ShouldBe(new PlacementDecision(id, PlacementSide.Right));
    }

    [Test]
    public void SearchesBreadthFirstAmongPreferredSlots()
    {
        var expectedId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), depth: 2, order: 0),
            Candidate(expectedId, depth: 1, order: 4),
        };

        var result = _strategy.SelectSlot(candidates, PlacementSide.Right);

        result.ParentAgentId.ShouldBe(expectedId);
        result.Side.ShouldBe(PlacementSide.Right);
    }

    [Test]
    public void SkipsCandidateWhosePreferredSideIsOccupied()
    {
        var expectedId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), depth: 0, order: 0, rightOccupied: true),
            Candidate(expectedId, depth: 1, order: 1),
        };

        var result = _strategy.SelectSlot(candidates, PlacementSide.Right);

        result.ParentAgentId.ShouldBe(expectedId);
        result.Side.ShouldBe(PlacementSide.Right);
    }

    [Test]
    public void FallsBackToBreadthFirstSlotWhenPreferredSideIsUnavailableEverywhere()
    {
        var expectedId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(expectedId, depth: 0, order: 0, rightOccupied: true),
            Candidate(Guid.NewGuid(), depth: 1, order: 1, rightOccupied: true),
        };

        var result = _strategy.SelectSlot(candidates, PlacementSide.Right);

        result.ShouldBe(new PlacementDecision(expectedId, PlacementSide.Left));
    }

    [Test]
    public void UsesLeftToRightBreadthFirstWhenNoPreferenceIsProvided()
    {
        var expectedId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), depth: 1, order: 2),
            Candidate(expectedId, depth: 1, order: 1),
        };

        var result = _strategy.SelectSlot(candidates);

        result.ShouldBe(new PlacementDecision(expectedId, PlacementSide.Left));
    }

    [Test]
    public void ThrowsWhenEveryCandidateIsFull()
    {
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), 0, 0, leftOccupied: true, rightOccupied: true),
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
