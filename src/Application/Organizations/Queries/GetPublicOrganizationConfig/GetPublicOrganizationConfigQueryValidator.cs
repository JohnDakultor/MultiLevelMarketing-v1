namespace modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig;

public sealed class GetPublicOrganizationConfigQueryValidator
    : AbstractValidator<GetPublicOrganizationConfigQuery>
{
    public GetPublicOrganizationConfigQueryValidator() =>
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
}
