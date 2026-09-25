namespace modular_mlm.Application.Catalog.Commands.ActivateCategory;

public sealed class ActivateCategoryCommandValidator : AbstractValidator<ActivateCategoryCommand>
{
    public ActivateCategoryCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
    }
}
