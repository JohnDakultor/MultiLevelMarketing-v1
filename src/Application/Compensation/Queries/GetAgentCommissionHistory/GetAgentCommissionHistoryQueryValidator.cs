namespace modular_mlm.Application.Compensation.Queries.GetAgentCommissionHistory;

public sealed class GetAgentCommissionHistoryQueryValidator
    : AbstractValidator<GetAgentCommissionHistoryQuery>
{
    public GetAgentCommissionHistoryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
