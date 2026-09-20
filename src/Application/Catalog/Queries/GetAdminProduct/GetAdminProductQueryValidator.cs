namespace modular_mlm.Application.Catalog.Queries.GetAdminProduct;

public sealed class GetAdminProductQueryValidator : AbstractValidator<GetAdminProductQuery>
{
    public GetAdminProductQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.ProductId).NotEmpty();
    }
}
