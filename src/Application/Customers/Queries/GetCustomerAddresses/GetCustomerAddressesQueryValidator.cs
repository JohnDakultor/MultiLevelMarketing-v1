namespace modular_mlm.Application.Customers.Queries.GetCustomerAddresses;

public sealed class GetCustomerAddressesQueryValidator
    : AbstractValidator<GetCustomerAddressesQuery>
{
    public GetCustomerAddressesQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
    }
}
