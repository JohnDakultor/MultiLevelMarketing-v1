namespace modular_mlm.Application.Compensation.Queries.GetPairingHistory;

public sealed class GetPairingHistoryQueryValidator : AbstractValidator<GetPairingHistoryQuery>
{
    public GetPairingHistoryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
        RuleFor(query => query.PeriodStart).LessThan(query => query.PeriodEnd);
        RuleFor(query => query.PeriodEnd)
            .Must((query, periodEnd) => periodEnd - query.PeriodStart <= TimeSpan.FromDays(366))
            .WithMessage("The pairing-history period cannot exceed 366 days.");
    }
}
