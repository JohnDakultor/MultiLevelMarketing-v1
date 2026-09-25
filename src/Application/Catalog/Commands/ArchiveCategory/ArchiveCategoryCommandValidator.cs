namespace modular_mlm.Application.Catalog.Commands.ArchiveCategory;

public sealed class ArchiveCategoryCommandValidator : AbstractValidator<ArchiveCategoryCommand>
{
    public ArchiveCategoryCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
    }
}
