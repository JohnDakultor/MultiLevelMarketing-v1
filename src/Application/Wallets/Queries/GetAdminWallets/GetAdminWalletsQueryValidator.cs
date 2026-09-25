namespace modular_mlm.Application.Wallets.Queries.GetAdminWallets;

public sealed class GetAdminWalletsQueryValidator : AbstractValidator<GetAdminWalletsQuery>
{
    public GetAdminWalletsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status)
            .IsInEnum()
            .When(query => query.Status.HasValue);
        RuleFor(query => query.Search)
            .MaximumLength(100)
            .Must(search => search is null || search.Trim().Length > 0)
            .WithMessage("Search cannot contain only whitespace.");
    }
}
