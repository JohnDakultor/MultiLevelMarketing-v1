namespace modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles;

public sealed class GetProductCommissionProfilesQueryValidator
    : AbstractValidator<GetProductCommissionProfilesQuery>
{
    public GetProductCommissionProfilesQueryValidator() =>
        RuleFor(query => query.OrganizationId).NotEmpty();
}
