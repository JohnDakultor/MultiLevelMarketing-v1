namespace modular_mlm.Application.Reporting;

internal static class ReportingPeriod
{
    public static void Configure<T>(
        AbstractValidator<T> validator,
        Func<T, DateTimeOffset> from,
        Func<T, DateTimeOffset> to
    )
    {
        validator.RuleFor(x => from(x)).LessThan(x => to(x));
        validator
            .RuleFor(x => to(x) - from(x))
            .LessThanOrEqualTo(TimeSpan.FromDays(366))
            .WithMessage("Reporting periods cannot exceed 366 days.");
    }
}
