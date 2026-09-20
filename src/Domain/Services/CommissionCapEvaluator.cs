using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Services;

public sealed record CommissionCapInput(
    decimal GrossCommission,
    decimal PreviouslyEarnedAmount,
    decimal MaximumAllowedAmount,
    bool CapEnabled = false
);

public sealed record CommissionCapResult(
    decimal GrossCommission,
    decimal PreviouslyEarnedAmount,
    decimal? RemainingCap,
    decimal PayableCommission,
    decimal CappedAmount,
    bool CapApplied,
    decimal PayableRatio
);

public sealed class CommissionCapEvaluator
{
    public CommissionCapResult Evaluate(CommissionCapInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input);

        if (!input.CapEnabled)
            return new CommissionCapResult(
                input.GrossCommission,
                input.PreviouslyEarnedAmount,
                null,
                input.GrossCommission,
                0m,
                false,
                CalculatePayableRatio(input.GrossCommission, input.GrossCommission)
            );

        var remainingCap = Math.Max(input.MaximumAllowedAmount - input.PreviouslyEarnedAmount, 0m);
        var payableCommission = Math.Min(input.GrossCommission, remainingCap);
        var cappedAmount = input.GrossCommission - payableCommission;

        return new CommissionCapResult(
            input.GrossCommission,
            input.PreviouslyEarnedAmount,
            remainingCap,
            payableCommission,
            cappedAmount,
            cappedAmount > 0m,
            CalculatePayableRatio(input.GrossCommission, payableCommission)
        );
    }

    private static decimal CalculatePayableRatio(
        decimal grossCommission,
        decimal payableCommission
    ) => grossCommission == 0m ? 0m : payableCommission / grossCommission;

    private static void Validate(CommissionCapInput input)
    {
        if (input.GrossCommission < 0m)
            throw new DomainInvariantException("Gross commission cannot be negative.");

        if (input.PreviouslyEarnedAmount < 0m)
            throw new DomainInvariantException("Previously earned commission cannot be negative.");

        if (input.MaximumAllowedAmount < 0m)
            throw new DomainInvariantException("Maximum allowed commission cannot be negative.");
    }
}
