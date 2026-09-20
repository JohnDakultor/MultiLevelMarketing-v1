namespace modular_mlm.Application.Compensation.Commands.ProcessOutstandingPaidOrderCompensation;

public sealed class ProcessOutstandingPaidOrderCompensationCommandValidator
    : AbstractValidator<ProcessOutstandingPaidOrderCompensationCommand>
{
    public ProcessOutstandingPaidOrderCompensationCommandValidator() =>
        RuleFor(command => command.BatchSize).InclusiveBetween(1, 500);
}
