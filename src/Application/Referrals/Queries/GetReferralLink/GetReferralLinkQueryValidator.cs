namespace modular_mlm.Application.Referrals.Queries.GetReferralLink;

public sealed class GetReferralLinkQueryValidator : AbstractValidator<GetReferralLinkQuery>
{
    public GetReferralLinkQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty().When(x => x.ProductId.HasValue);
    }
}
