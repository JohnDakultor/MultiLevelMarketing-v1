namespace modular_mlm.Application.Network.Commands.ReactivateAgent;

public sealed class ReactivateAgentCommandValidator : AbstractValidator<ReactivateAgentCommand>
{
    public ReactivateAgentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
