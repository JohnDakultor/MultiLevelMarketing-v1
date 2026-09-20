namespace modular_mlm.Application.Identity.Commands.RevokeCurrentSession;

public sealed class RevokeCurrentSessionCommandValidator
    : AbstractValidator<RevokeCurrentSessionCommand>
{
    public RevokeCurrentSessionCommandValidator()
    {
        RuleFor(command => command.SessionId).NotEmpty();
    }
}
