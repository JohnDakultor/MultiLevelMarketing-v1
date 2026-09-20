namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrders;

public sealed class GetAttributedOrdersQueryValidator : AbstractValidator<GetAttributedOrdersQuery>
{
    public GetAttributedOrdersQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status!.Value).IsInEnum().When(query => query.Status.HasValue);
        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null || query.From <= query.To)
            .WithMessage("From must not be later than To.");
    }
}
