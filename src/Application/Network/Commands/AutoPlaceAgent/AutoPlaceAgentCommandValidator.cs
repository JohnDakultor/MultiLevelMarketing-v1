using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Commands.AutoPlaceAgent;

public sealed class AutoPlaceAgentCommandValidator : AbstractValidator<AutoPlaceAgentCommand>
{
    public AutoPlaceAgentCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AgentId).NotEmpty();
        RuleFor(command => command.Strategy!.Value)
            .IsInEnum()
            .When(command => command.Strategy.HasValue);
        RuleFor(command => command.PreferredSide)
            .NotNull()
            .When(command => command.Strategy == PlacementStrategyType.PreferredLeg);
    }
}
