namespace modular_mlm.Application.Network.Commands.ApproveAgent;

public sealed class ApproveAgentCommandValidator : AbstractValidator<ApproveAgentCommand>
{
    public ApproveAgentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
