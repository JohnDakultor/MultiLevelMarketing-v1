namespace modular_mlm.Application.Compensation.Queries.GetCommissionPlans;

public sealed class GetCommissionPlansQueryValidator : AbstractValidator<GetCommissionPlansQuery>
{
    public GetCommissionPlansQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
    }
}
