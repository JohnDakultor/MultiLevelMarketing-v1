namespace modular_mlm.Application.Network.Commands.RejectAgent;

public sealed class RejectAgentCommandValidator : AbstractValidator<RejectAgentCommand>
{
    public RejectAgentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
