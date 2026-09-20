using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Compensation.Services;

public sealed record DirectSalesCommissionResult(
    decimal BaseAmount,
    decimal Rate,
    decimal GrossAmount
);

public static class DirectSalesCommissionCalculator
{
    public static DirectSalesCommissionResult Calculate(
        decimal commissionableAmount,
        decimal planRate,
        decimal? productRateOverride = null
    )
    {
        if (commissionableAmount < 0)
            throw new DomainInvariantException("Commissionable amount cannot be negative.");
        if (planRate is < 0 or > 1 || productRateOverride is < 0 or > 1)
            throw new DomainInvariantException("Commission rates must be between zero and one.");

        var effectiveRate = productRateOverride ?? planRate;
        var grossAmount = decimal.Round(
            commissionableAmount * effectiveRate,
            2,
            MidpointRounding.AwayFromZero
        );

        return new DirectSalesCommissionResult(commissionableAmount, effectiveRate, grossAmount);
    }
}
