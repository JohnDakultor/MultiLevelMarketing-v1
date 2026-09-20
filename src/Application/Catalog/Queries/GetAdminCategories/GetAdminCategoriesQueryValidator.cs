namespace modular_mlm.Application.Catalog.Queries.GetAdminCategories;

public sealed class GetAdminCategoriesQueryValidator : AbstractValidator<GetAdminCategoriesQuery>
{
    public GetAdminCategoriesQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
