namespace modular_mlm.Application.Referrals.Queries.GetAgentStorefront;

public sealed class GetAgentStorefrontQueryValidator : AbstractValidator<GetAgentStorefrontQuery>
{
    public GetAgentStorefrontQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ReferralCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
