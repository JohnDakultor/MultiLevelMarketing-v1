namespace modular_mlm.Application.Network.Commands.PlaceAgent;

public sealed class PlaceAgentCommandValidator : AbstractValidator<PlaceAgentCommand>
{
    public PlaceAgentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.ParentAgentId).NotEmpty().NotEqual(x => x.AgentId);
        RuleFor(x => x.Side).IsInEnum();
    }
}
