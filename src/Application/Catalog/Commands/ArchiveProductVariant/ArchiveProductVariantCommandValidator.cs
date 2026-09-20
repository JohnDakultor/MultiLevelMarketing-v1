namespace modular_mlm.Application.Catalog.Commands.ArchiveProductVariant;

public sealed class ArchiveProductVariantCommandValidator
    : AbstractValidator<ArchiveProductVariantCommand>
{
    public ArchiveProductVariantCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
