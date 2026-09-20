namespace modular_mlm.Application.Payouts.Queries.GetPayoutHistory;

public sealed class GetPayoutHistoryQueryValidator : AbstractValidator<GetPayoutHistoryQuery>
{
    public GetPayoutHistoryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
