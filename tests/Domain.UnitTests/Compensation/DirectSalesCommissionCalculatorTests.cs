using modular_mlm.Domain.Compensation.Services;
using modular_mlm.Domain.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Compensation;

public sealed class DirectSalesCommissionCalculatorTests
{
    [Test]
    public void ShouldCalculateGrossCommissionUsingPlanRate()
    {
        var result = DirectSalesCommissionCalculator.Calculate(1_000m, 0.10m);

        result.BaseAmount.ShouldBe(1_000m);
        result.Rate.ShouldBe(0.10m);
        result.GrossAmount.ShouldBe(100m);
    }

    [Test]
    public void ShouldUseProductRateOverride()
    {
        var result = DirectSalesCommissionCalculator.Calculate(1_000m, 0.10m, 0.15m);

        result.Rate.ShouldBe(0.15m);
        result.GrossAmount.ShouldBe(150m);
    }

    [Test]
    public void ShouldRoundPhilippinePesoAmountsAwayFromZeroToTwoDecimals()
    {
        var result = DirectSalesCommissionCalculator.Calculate(10.05m, 0.10m);

        result.GrossAmount.ShouldBe(1.01m);
    }

    [Test]
    public void ShouldRejectAnInvalidRate()
    {
        Should.Throw<DomainInvariantException>(() =>
            DirectSalesCommissionCalculator.Calculate(1_000m, 1.01m)
        );
    }
}
