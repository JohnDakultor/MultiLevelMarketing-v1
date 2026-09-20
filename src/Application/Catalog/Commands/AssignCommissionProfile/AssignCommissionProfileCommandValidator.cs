namespace modular_mlm.Application.Catalog.Commands.AssignCommissionProfile;

public sealed class AssignCommissionProfileCommandValidator
    : AbstractValidator<AssignCommissionProfileCommand>
{
    public AssignCommissionProfileCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();
    }
}
