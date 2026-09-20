namespace modular_mlm.Application.Catalog.Commands.CreateProductVariant;

public sealed class CreateProductVariantCommandValidator
    : AbstractValidator<CreateProductVariantCommand>
{
    public CreateProductVariantCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.BusinessVolume).GreaterThanOrEqualTo(0).PrecisionScale(18, 4, true);
        RuleFor(x => x.InitialOnHandQuantity).InclusiveBetween(0, int.MaxValue);
        RuleFor(x => x.InitialOnHandQuantity)
            .Equal(0)
            .When(x => !x.StockKeepingEnabled)
            .WithMessage("Initial stock must be zero when stock keeping is disabled.");
    }
}
