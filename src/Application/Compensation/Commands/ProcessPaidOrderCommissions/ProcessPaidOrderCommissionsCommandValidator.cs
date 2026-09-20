namespace modular_mlm.Application.Compensation.Commands.ProcessPaidOrderCommissions;

public sealed class ProcessPaidOrderCommissionsCommandValidator
    : AbstractValidator<ProcessPaidOrderCommissionsCommand>
{
    public ProcessPaidOrderCommissionsCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
    }
}
