using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Compensation;

public sealed class BinaryPairingRule
{
    private BinaryPairingRule() { }

    public bool Enabled { get; private set; }
    public PairingCalculationType CalculationType { get; private set; }
    public decimal? PairingRate { get; private set; }
    public decimal? PairUnitBv { get; private set; }
    public decimal? FixedPairAmount { get; private set; }
    public ProcessingFrequency ProcessingFrequency { get; private set; }
    public bool CarryForwardEnabled { get; private set; }
    public string VolumeExpiryPolicy { get; private set; } = string.Empty;
    public string CapPolicy { get; private set; } = string.Empty;
    public Guid? QualificationRuleSetId { get; private set; }

    public static BinaryPairingRule Disabled() =>
        new() { VolumeExpiryPolicy = "None", CapPolicy = "PreserveVolume" };

    public static BinaryPairingRule Percentage(
        decimal rate,
        ProcessingFrequency frequency,
        bool carryForward = true
    )
    {
        if (rate is <= 0 or > 1)
            throw new DomainInvariantException(
                "Pairing rate must be greater than zero and no more than one."
            );
        return new BinaryPairingRule
        {
            Enabled = true,
            CalculationType = PairingCalculationType.PercentageMatchedVolume,
            PairingRate = rate,
            ProcessingFrequency = frequency,
            CarryForwardEnabled = carryForward,
            VolumeExpiryPolicy = "None",
            CapPolicy = "PreserveVolume",
        };
    }

    public static BinaryPairingRule FixedPerPair(
        decimal pairUnitBv,
        decimal fixedPairAmount,
        ProcessingFrequency frequency,
        bool carryForward = true
    )
    {
        if (pairUnitBv <= 0m)
            throw new DomainInvariantException("Pair-unit BV must be positive.");
        if (fixedPairAmount <= 0m)
            throw new DomainInvariantException("Fixed pair amount must be positive.");

        return new BinaryPairingRule
        {
            Enabled = true,
            CalculationType = PairingCalculationType.FixedPerPair,
            PairUnitBv = pairUnitBv,
            FixedPairAmount = fixedPairAmount,
            ProcessingFrequency = frequency,
            CarryForwardEnabled = carryForward,
            VolumeExpiryPolicy = "None",
            CapPolicy = "PreserveVolume",
        };
    }
}
