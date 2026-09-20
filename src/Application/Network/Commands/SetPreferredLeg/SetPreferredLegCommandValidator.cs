namespace modular_mlm.Application.Network.Commands.SetPreferredLeg;

public sealed class SetPreferredLegCommandValidator : AbstractValidator<SetPreferredLegCommand>
{
    public SetPreferredLegCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.PreferredLeg).IsInEnum();
    }
}
