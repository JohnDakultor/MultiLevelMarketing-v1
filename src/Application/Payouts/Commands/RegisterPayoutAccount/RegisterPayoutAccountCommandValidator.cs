namespace modular_mlm.Application.Payouts.Commands.RegisterPayoutAccount;

public sealed class RegisterPayoutAccountCommandValidator
    : AbstractValidator<RegisterPayoutAccountCommand>
{
    public RegisterPayoutAccountCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AgentId).NotEmpty();
        RuleFor(command => command.Method).NotEmpty().MaximumLength(50);
        RuleFor(command => command.AccountName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.AccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(command => command.BankCode).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Rail).Must(rail => rail is "instapay" or "pesonet");
    }
}
