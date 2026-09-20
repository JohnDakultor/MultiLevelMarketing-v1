namespace modular_mlm.Application.Monitoring.Queries.GetOperationalHealth;

public sealed class GetOperationalHealthQueryValidator
    : AbstractValidator<GetOperationalHealthQuery>
{
    public GetOperationalHealthQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
    }
}
