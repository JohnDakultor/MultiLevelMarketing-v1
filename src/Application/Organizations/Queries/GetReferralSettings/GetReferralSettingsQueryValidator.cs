namespace modular_mlm.Application.Organizations.Queries.GetReferralSettings;

public sealed class GetReferralSettingsQueryValidator : AbstractValidator<GetReferralSettingsQuery>
{
    public GetReferralSettingsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
    }
}
