namespace modular_mlm.Application.Catalog.Commands.RenameCategory;

public sealed class RenameCategoryCommandValidator : AbstractValidator<RenameCategoryCommand>
{
    public RenameCategoryCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().Must(name => name.Trim().Length <= 200)
            .WithMessage("Name must not exceed 200 characters.");
    }
}
