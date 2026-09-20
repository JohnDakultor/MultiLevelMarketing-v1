namespace modular_mlm.Application.Compensation.Queries.GetAgentEarningsSummary;

public sealed class GetAgentEarningsSummaryQueryValidator
    : AbstractValidator<GetAgentEarningsSummaryQuery>
{
    public GetAgentEarningsSummaryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
    }
}
