using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Network.Services;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class BinaryPairingCalculatorTests
{
    [Test]
    public void CalculatesMatchedVolumeCommissionAndCarryForward()
    {
        var result = BinaryPairingCalculator.Calculate(12_000m, 8_000m, 0.10m);
        result.MatchedVolume.ShouldBe(8_000m);
        result.Commission.ShouldBe(800m);
        result.LeftRemaining.ShouldBe(4_000m);
        result.RightRemaining.ShouldBe(0m);
    }

    [Test]
    public void CalculatesOnlyCompleteFixedPairsAndCarriesTheRemainder()
    {
        var rule = BinaryPairingRule.FixedPerPair(100m, 25m, ProcessingFrequency.Weekly);

        var result = BinaryPairingCalculator.Calculate(550m, 380m, rule);

        result.CompletedPairs.ShouldBe(3);
        result.MatchedVolume.ShouldBe(300m);
        result.LeftConsumed.ShouldBe(300m);
        result.RightConsumed.ShouldBe(300m);
        result.GrossCommission.ShouldBe(75m);
        result.LeftRemaining.ShouldBe(250m);
        result.RightRemaining.ShouldBe(80m);
    }
}
