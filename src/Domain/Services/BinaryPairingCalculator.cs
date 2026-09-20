using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Network.Services;

public sealed record BinaryPairingCalculationResult(
    decimal MatchedVolume,
    decimal LeftConsumed,
    decimal RightConsumed,
    decimal GrossCommission,
    decimal LeftRemaining,
    decimal RightRemaining,
    long CompletedPairs
);

public static class BinaryPairingCalculator
{
    public static BinaryPairingCalculationResult Calculate(
        decimal leftAvailable,
        decimal rightAvailable,
        BinaryPairingRule rule
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (leftAvailable < 0m || rightAvailable < 0m)
            throw new DomainInvariantException("Available leg volume cannot be negative.");
        if (!rule.Enabled)
            return new(0m, 0m, 0m, 0m, leftAvailable, rightAvailable, 0);

        var availableToMatch = Math.Min(leftAvailable, rightAvailable);
        decimal consumed;
        decimal commission;
        long pairs;

        switch (rule.CalculationType)
        {
            case PairingCalculationType.PercentageMatchedVolume:
                if (rule.PairingRate is not > 0m or > 1m)
                    throw new DomainInvariantException(
                        "A valid percentage pairing rate is required."
                    );
                consumed = availableToMatch;
                commission = decimal.Round(
                    consumed * rule.PairingRate.Value,
                    2,
                    MidpointRounding.AwayFromZero
                );
                pairs = 0;
                break;
            case PairingCalculationType.FixedPerPair:
                if (rule.PairUnitBv is not > 0m || rule.FixedPairAmount is not > 0m)
                    throw new DomainInvariantException(
                        "A pair unit and fixed amount are required."
                    );
                pairs = decimal.ToInt64(decimal.Floor(availableToMatch / rule.PairUnitBv.Value));
                consumed = pairs * rule.PairUnitBv.Value;
                commission = decimal.Round(
                    pairs * rule.FixedPairAmount.Value,
                    2,
                    MidpointRounding.AwayFromZero
                );
                break;
            default:
                throw new DomainInvariantException("The pairing calculation type is unsupported.");
        }

        return new(
            consumed,
            consumed,
            consumed,
            commission,
            leftAvailable - consumed,
            rightAvailable - consumed,
            pairs
        );
    }

    public static (
        decimal MatchedVolume,
        decimal Commission,
        decimal LeftRemaining,
        decimal RightRemaining
    ) Calculate(decimal leftAvailable, decimal rightAvailable, decimal rate)
    {
        if (leftAvailable < 0 || rightAvailable < 0)
            throw new ArgumentOutOfRangeException(nameof(leftAvailable));
        if (rate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(rate));
        var matched = Math.Min(leftAvailable, rightAvailable);
        return (
            matched,
            decimal.Round(matched * rate, 2),
            leftAvailable - matched,
            rightAvailable - matched
        );
    }
}
