namespace modular_mlm.Application.Network.Queries.GetAdminAgentDetails;

public sealed class GetAdminAgentDetailsQueryValidator
    : AbstractValidator<GetAdminAgentDetailsQuery>
{
    public GetAdminAgentDetailsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
    }
}
