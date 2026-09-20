namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings;

public sealed class GetAdminOrganizationSettingsQueryValidator
    : AbstractValidator<GetAdminOrganizationSettingsQuery>
{
    public GetAdminOrganizationSettingsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
    }
}
