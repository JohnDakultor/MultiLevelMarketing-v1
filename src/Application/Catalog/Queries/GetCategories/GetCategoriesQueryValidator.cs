namespace modular_mlm.Application.Catalog.Queries.GetCategories;

public sealed class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
    }
}
