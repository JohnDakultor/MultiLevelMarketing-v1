namespace modular_mlm.Application.Payouts.Queries.GetPayoutAccounts;

public sealed class GetPayoutAccountsQueryValidator : AbstractValidator<GetPayoutAccountsQuery>
{
    public GetPayoutAccountsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
    }
}
