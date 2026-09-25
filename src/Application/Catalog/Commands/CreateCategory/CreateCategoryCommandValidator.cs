namespace modular_mlm.Application.Catalog.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.Name)
            .NotEmpty()
            .Must(name => name.Trim().Length <= 200)
            .WithMessage("Name must not exceed 200 characters.");
        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(200)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage(
                "Slug must contain lowercase letters or numbers separated by single hyphens."
            );
    }
}
