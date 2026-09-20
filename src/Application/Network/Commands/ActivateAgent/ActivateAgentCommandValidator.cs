namespace modular_mlm.Application.Network.Commands.ActivateAgent;

public sealed class ActivateAgentCommandValidator : AbstractValidator<ActivateAgentCommand>
{
    public ActivateAgentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
