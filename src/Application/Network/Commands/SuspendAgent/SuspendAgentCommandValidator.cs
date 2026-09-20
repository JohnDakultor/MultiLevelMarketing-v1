namespace modular_mlm.Application.Network.Commands.SuspendAgent;

public sealed class SuspendAgentCommandValidator : AbstractValidator<SuspendAgentCommand>
{
    public SuspendAgentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
