namespace modular_mlm.Application.Commerce.Queries.GetMyOrders;

public sealed class GetMyOrdersQueryValidator : AbstractValidator<GetMyOrdersQuery>
{
    public GetMyOrdersQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);

        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
    }
}
