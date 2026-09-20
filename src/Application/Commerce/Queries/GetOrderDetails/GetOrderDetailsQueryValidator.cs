namespace modular_mlm.Application.Commerce.Queries.GetOrderDetails;

public sealed class GetOrderDetailsQueryValidator : AbstractValidator<GetOrderDetailsQuery>
{
    public GetOrderDetailsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.OrderId).NotEmpty();
    }
}
