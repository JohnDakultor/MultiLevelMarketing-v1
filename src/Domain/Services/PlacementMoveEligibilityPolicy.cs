using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Domain.Services;

public sealed record PlacementMoveEligibilityDecision(
    bool IsEligible,
    string ReasonCode,
    string Reason
);

public sealed class PlacementMoveEligibilityPolicy
{
    public PlacementMoveEligibilityDecision Evaluate(PlacementCommitmentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (
            snapshot.DescendantCount < 0
            || snapshot.AttributedPaidOrderCount < 0
            || snapshot.BinaryVolumeEntryCount < 0
            || snapshot.CommissionCount < 0
        )
            throw new DomainInvariantException("Placement commitment counts cannot be negative.");

        if (snapshot.DescendantCount > 0)
            return Ineligible(
                "HAS_DESCENDANTS",
                "An Agent with placement descendants cannot be moved."
            );
        if (snapshot.AttributedPaidOrderCount > 0)
            return Ineligible(
                "HAS_PAID_SALES",
                "An Agent with attributed paid orders cannot be moved."
            );
        if (snapshot.BinaryVolumeEntryCount > 0)
            return Ineligible(
                "HAS_BINARY_VOLUME",
                "An Agent with binary volume history cannot be moved."
            );
        if (snapshot.CommissionCount > 0)
            return Ineligible(
                "HAS_COMMISSIONS",
                "An Agent with commission history cannot be moved."
            );

        return new PlacementMoveEligibilityDecision(
            true,
            "ELIGIBLE",
            "The placement is uncommitted and may be moved."
        );
    }

    private static PlacementMoveEligibilityDecision Ineligible(string code, string reason) =>
        new(false, code, reason);
}
