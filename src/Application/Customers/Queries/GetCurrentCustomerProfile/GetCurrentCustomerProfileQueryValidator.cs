namespace modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile;

public sealed class GetCurrentCustomerProfileQueryValidator
    : AbstractValidator<GetCurrentCustomerProfileQuery>
{
    public GetCurrentCustomerProfileQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
    }
}
