namespace modular_mlm.Application.Customers.Queries.GetAdminCustomers;

public sealed class GetAdminCustomersQueryValidator : AbstractValidator<GetAdminCustomersQuery>
{
    public GetAdminCustomersQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(200);
        RuleFor(query => query.Sort).IsInEnum();
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
    }
}
