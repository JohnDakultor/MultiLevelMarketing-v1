namespace modular_mlm.Application.Network.Queries.GetAgentProfile;

public sealed class GetAgentProfileQueryValidator : AbstractValidator<GetAgentProfileQuery>
{
    public GetAgentProfileQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
    }
}
