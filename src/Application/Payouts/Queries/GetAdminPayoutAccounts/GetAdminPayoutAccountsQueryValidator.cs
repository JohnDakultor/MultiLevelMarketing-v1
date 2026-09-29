namespace modular_mlm.Application.Payouts.Queries.GetAdminPayoutAccounts;

public sealed class GetAdminPayoutAccountsQueryValidator
    : AbstractValidator<GetAdminPayoutAccountsQuery>
{
    public GetAdminPayoutAccountsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
    }
}
