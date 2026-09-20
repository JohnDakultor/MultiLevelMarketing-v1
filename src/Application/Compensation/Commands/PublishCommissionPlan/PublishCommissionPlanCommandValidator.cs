namespace modular_mlm.Application.Compensation.Commands.PublishCommissionPlan;

public sealed class PublishCommissionPlanCommandValidator
    : AbstractValidator<PublishCommissionPlanCommand>
{
    public PublishCommissionPlanCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.CommissionPlanId).NotEmpty();
    }
}
