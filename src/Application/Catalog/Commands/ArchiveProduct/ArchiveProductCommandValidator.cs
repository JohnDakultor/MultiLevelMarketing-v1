namespace modular_mlm.Application.Catalog.Commands.ArchiveProduct;

public sealed class ArchiveProductCommandValidator : AbstractValidator<ArchiveProductCommand>
{
    public ArchiveProductCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
