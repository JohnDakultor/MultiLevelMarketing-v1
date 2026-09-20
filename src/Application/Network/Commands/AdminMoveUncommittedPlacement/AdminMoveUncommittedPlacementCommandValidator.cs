namespace modular_mlm.Application.Network.Commands.AdminMoveUncommittedPlacement;

public sealed class AdminMoveUncommittedPlacementCommandValidator
    : AbstractValidator<AdminMoveUncommittedPlacementCommand>
{
    public AdminMoveUncommittedPlacementCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AgentId).NotEmpty();
        RuleFor(command => command.NewParentAgentId)
            .NotEmpty()
            .NotEqual(command => command.AgentId);
        RuleFor(command => command.ExpectedCurrentParentAgentId).NotEmpty();
        RuleFor(command => command.NewSide).IsInEnum();
        RuleFor(command => command.ExpectedCurrentSide).IsInEnum();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}
