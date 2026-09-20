namespace modular_mlm.Application.Identity.Queries.GetAdministrators;

public sealed class GetAdministratorsQueryValidator : AbstractValidator<GetAdministratorsQuery>
{
    public GetAdministratorsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
