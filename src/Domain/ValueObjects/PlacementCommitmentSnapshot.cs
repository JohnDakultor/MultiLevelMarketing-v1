namespace modular_mlm.Domain.ValueObjects;

public sealed record PlacementCommitmentSnapshot(
    int DescendantCount,
    int AttributedPaidOrderCount,
    int BinaryVolumeEntryCount,
    int CommissionCount
)
{
    public bool HasFinancialActivity =>
        AttributedPaidOrderCount > 0 || BinaryVolumeEntryCount > 0 || CommissionCount > 0;
}
