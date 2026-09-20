namespace modular_mlm.Application.Compensation.Queries.GetCommissionDetails;

public sealed class GetCommissionDetailsQueryValidator
    : AbstractValidator<GetCommissionDetailsQuery>
{
    public GetCommissionDetailsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
        RuleFor(query => query.CommissionId).NotEmpty();
    }
}
