namespace modular_mlm.Application.Catalog.Queries.GetAdminProducts;

public sealed class GetAdminProductsQueryValidator : AbstractValidator<GetAdminProductsQuery>
{
    public GetAdminProductsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
