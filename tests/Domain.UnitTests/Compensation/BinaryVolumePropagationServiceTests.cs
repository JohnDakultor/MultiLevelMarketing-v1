using modular_mlm.Domain.Compensation.Services;
using modular_mlm.Domain.Network;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Compensation;

public sealed class BinaryVolumePropagationServiceTests
{
    [Test]
    public void ShouldCreditEveryAncestorUsingTheDescendantsFirstLeg()
    {
        var organizationId = Guid.NewGuid();
        var sourceAgentId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var directParentId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var ancestors = new[]
        {
            PlacementClosure.Create(
                organizationId,
                directParentId,
                sourceAgentId,
                1,
                PlacementSide.Right
            ),
            PlacementClosure.Create(organizationId, rootId, sourceAgentId, 2, PlacementSide.Left),
        };

        var credits = BinaryVolumePropagationService.CreateCredits(
            organizationId,
            sourceAgentId,
            orderItemId,
            125m,
            ancestors,
            DateTimeOffset.UtcNow
        );

        credits.Count.ShouldBe(2);
        credits
            .Single(credit => credit.OwnerAgentId == directParentId)
            .Side.ShouldBe(PlacementSide.Right);
        credits.Single(credit => credit.OwnerAgentId == rootId).Side.ShouldBe(PlacementSide.Left);
        credits.ShouldAllBe(credit => credit.Volume == 125m);
    }

    [Test]
    public void ShouldNotCreateCreditsWhenVolumeIsZero()
    {
        var credits = BinaryVolumePropagationService.CreateCredits(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0m,
            [],
            DateTimeOffset.UtcNow
        );

        credits.ShouldBeEmpty();
    }
}
