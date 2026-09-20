namespace modular_mlm.Application.Network.Queries.GetAgentApplication;

public sealed class GetAgentApplicationQueryValidator : AbstractValidator<GetAgentApplicationQuery>
{
    public GetAgentApplicationQueryValidator() => RuleFor(query => query.OrganizationId).NotEmpty();
}
