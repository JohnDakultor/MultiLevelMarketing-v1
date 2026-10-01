namespace modular_mlm.Application.Organizations.Queries.GetOrganizationProvisioning;

public sealed class GetOrganizationProvisioningQueryValidator
    : AbstractValidator<GetOrganizationProvisioningQuery>
{
    public GetOrganizationProvisioningQueryValidator() =>
        RuleFor(query => query.Slug)
            .NotEmpty()
            .MaximumLength(200)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");
}
