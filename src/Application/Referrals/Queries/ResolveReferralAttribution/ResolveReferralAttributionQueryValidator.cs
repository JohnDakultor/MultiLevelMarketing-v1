namespace modular_mlm.Application.Referrals.Queries.ResolveReferralAttribution;

public sealed class ResolveReferralAttributionQueryValidator
    : AbstractValidator<ResolveReferralAttributionQuery>
{
    public ResolveReferralAttributionQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.ReferralCode).NotEmpty().MaximumLength(64);
    }
}
