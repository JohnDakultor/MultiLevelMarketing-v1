namespace modular_mlm.Application.Compensation.Queries.GetCommissionPlan;

public sealed class GetCommissionPlanQueryValidator : AbstractValidator<GetCommissionPlanQuery>
{
    public GetCommissionPlanQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.CommissionPlanId).NotEmpty();
    }
}
