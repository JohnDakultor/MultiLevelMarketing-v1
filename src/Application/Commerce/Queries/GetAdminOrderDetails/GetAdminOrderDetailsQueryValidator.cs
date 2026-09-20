namespace modular_mlm.Application.Commerce.Queries.GetAdminOrderDetails;

public sealed class GetAdminOrderDetailsQueryValidator
    : AbstractValidator<GetAdminOrderDetailsQuery>
{
    public GetAdminOrderDetailsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.OrderId).NotEmpty();
    }
}
