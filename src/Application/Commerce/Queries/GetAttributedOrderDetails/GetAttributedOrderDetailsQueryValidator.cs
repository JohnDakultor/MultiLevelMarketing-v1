namespace modular_mlm.Application.Commerce.Queries.GetAttributedOrderDetails;

public sealed class GetAttributedOrderDetailsQueryValidator
    : AbstractValidator<GetAttributedOrderDetailsQuery>
{
    public GetAttributedOrderDetailsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.OrderId).NotEmpty();
    }
}
