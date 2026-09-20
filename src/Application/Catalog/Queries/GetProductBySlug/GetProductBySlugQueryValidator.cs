namespace modular_mlm.Application.Catalog.Queries.GetProductBySlug;

public sealed class GetProductBySlugQueryValidator : AbstractValidator<GetProductBySlugQuery>
{
    public GetProductBySlugQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
    }
}
