namespace modular_mlm.Application.Wallets.Commands.CreateWalletAdjustment;

public sealed class CreateWalletAdjustmentCommandValidator
    : AbstractValidator<CreateWalletAdjustmentCommand>
{
    public CreateWalletAdjustmentCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AgentId).NotEmpty();
        RuleFor(command => command.Direction).IsInEnum();
        RuleFor(command => command.Amount)
            .GreaterThan(0m)
            .LessThan(10_000_000_000_000_000m)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(command => command.Currency).NotEmpty().Matches("^[A-Za-z]{3}$");
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .Must(value => value.All(character => !char.IsControl(character)))
            .WithMessage("Reason cannot contain control characters.");
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128)
            .Must(value => value.All(character => !char.IsControl(character)))
            .WithMessage("Idempotency key cannot contain control characters.");
    }
}
