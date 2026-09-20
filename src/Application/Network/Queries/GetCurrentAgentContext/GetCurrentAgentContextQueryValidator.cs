namespace modular_mlm.Application.Network.Queries.GetCurrentAgentContext;

public sealed class GetCurrentAgentContextQueryValidator
    : AbstractValidator<GetCurrentAgentContextQuery>
{
    public GetCurrentAgentContextQueryValidator() =>
        RuleFor(query => query.OrganizationId).NotEmpty();
}
