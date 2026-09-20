namespace modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary;

public sealed class GetAgentProductSalesSummaryQueryValidator
    : AbstractValidator<GetAgentProductSalesSummaryQuery>
{
    public GetAgentProductSalesSummaryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null || query.From <= query.To)
            .WithMessage("From must not be later than To.");
    }
}
