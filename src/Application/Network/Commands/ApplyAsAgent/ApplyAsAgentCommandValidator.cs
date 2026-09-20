namespace modular_mlm.Application.Network.Commands.ApplyAsAgent;

public sealed class ApplyAsAgentCommandValidator : AbstractValidator<ApplyAsAgentCommand>
{
    public ApplyAsAgentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.SponsorAgentId).NotEqual(Guid.Empty).When(x => x.SponsorAgentId.HasValue);
    }
}
