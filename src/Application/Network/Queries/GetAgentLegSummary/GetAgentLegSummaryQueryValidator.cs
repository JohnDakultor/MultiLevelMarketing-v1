namespace modular_mlm.Application.Network.Queries.GetAgentLegSummary;

public sealed class GetAgentLegSummaryQueryValidator : AbstractValidator<GetAgentLegSummaryQuery>
{
    public GetAgentLegSummaryQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
