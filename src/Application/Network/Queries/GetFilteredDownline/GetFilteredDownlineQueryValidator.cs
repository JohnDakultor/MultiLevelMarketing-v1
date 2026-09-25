namespace modular_mlm.Application.Network.Queries.GetFilteredDownline;

public sealed class GetFilteredDownlineQueryValidator
    : AbstractValidator<GetFilteredDownlineQuery>
{
    public GetFilteredDownlineQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.MaxDepth)
            .GreaterThanOrEqualTo(1)
            .When(query => query.MaxDepth.HasValue);
        RuleFor(query => query.FirstLeg)
            .IsInEnum()
            .When(query => query.FirstLeg.HasValue);
        RuleFor(query => query.Status)
            .IsInEnum()
            .When(query => query.Status.HasValue);
        RuleFor(query => query.Search)
            .MaximumLength(100)
            .Must(search => search is null || search.Trim().Length > 0)
            .WithMessage("Search cannot contain only whitespace.");
    }
}
