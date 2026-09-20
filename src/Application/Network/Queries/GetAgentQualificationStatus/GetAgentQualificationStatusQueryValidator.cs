namespace modular_mlm.Application.Network.Queries.GetAgentQualificationStatus;

public sealed class GetAgentQualificationStatusQueryValidator
    : AbstractValidator<GetAgentQualificationStatusQuery>
{
    public GetAgentQualificationStatusQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
    }
}
