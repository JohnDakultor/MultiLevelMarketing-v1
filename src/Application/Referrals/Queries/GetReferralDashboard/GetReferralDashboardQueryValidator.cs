namespace modular_mlm.Application.Referrals.Queries.GetReferralDashboard;

public sealed class GetReferralDashboardQueryValidator
    : AbstractValidator<GetReferralDashboardQuery>
{
    public GetReferralDashboardQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
