namespace modular_mlm.Application.Catalog.Queries.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.CategorySlug)
            .MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(x => !string.IsNullOrWhiteSpace(x.CategorySlug));
        RuleFor(x => x.MinimumPrice).GreaterThanOrEqualTo(0).When(x => x.MinimumPrice.HasValue);
        RuleFor(x => x.MaximumPrice).GreaterThanOrEqualTo(0).When(x => x.MaximumPrice.HasValue);
        RuleFor(x => x)
            .Must(x => !x.MinimumPrice.HasValue || !x.MaximumPrice.HasValue || x.MinimumPrice <= x.MaximumPrice)
            .WithMessage("Minimum price cannot exceed maximum price.");
        RuleFor(x => x.Availability).IsInEnum();
        RuleFor(x => x.Sort).IsInEnum();
    }
}
