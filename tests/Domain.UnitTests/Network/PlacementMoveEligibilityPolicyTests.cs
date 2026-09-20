using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class PlacementMoveEligibilityPolicyTests
{
    private readonly PlacementMoveEligibilityPolicy _policy = new();

    [Test]
    public void UncommittedLeafIsEligible()
    {
        var result = _policy.Evaluate(new PlacementCommitmentSnapshot(0, 0, 0, 0));

        result.IsEligible.ShouldBeTrue();
        result.ReasonCode.ShouldBe("ELIGIBLE");
        result.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [TestCase(1, 0, 0, 0, "HAS_DESCENDANTS")]
    [TestCase(0, 1, 0, 0, "HAS_PAID_SALES")]
    [TestCase(0, 0, 1, 0, "HAS_BINARY_VOLUME")]
    [TestCase(0, 0, 0, 1, "HAS_COMMISSIONS")]
    public void CommitmentHistoryRejectsMovement(
        int descendants,
        int paidOrders,
        int volumeEntries,
        int commissions,
        string expectedCode
    )
    {
        var result = _policy.Evaluate(
            new PlacementCommitmentSnapshot(descendants, paidOrders, volumeEntries, commissions)
        );

        result.IsEligible.ShouldBeFalse();
        result.ReasonCode.ShouldBe(expectedCode);
        result.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void NegativeCountsAreRejected()
    {
        Should.Throw<DomainInvariantException>(() =>
            _policy.Evaluate(new PlacementCommitmentSnapshot(-1, 0, 0, 0))
        );
    }
}
