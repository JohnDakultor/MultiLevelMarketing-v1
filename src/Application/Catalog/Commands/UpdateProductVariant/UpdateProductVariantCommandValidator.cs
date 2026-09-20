using System.Text.Json;

namespace modular_mlm.Application.Catalog.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.BusinessVolume).GreaterThanOrEqualTo(0).PrecisionScale(18, 4, true);
        RuleFor(x => x.AttributesJson).MaximumLength(10000);
        RuleFor(x => x.AttributesJson)
            .Must(BeValidJson)
            .WithMessage("AttributesJson must contain valid JSON.");
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }

    private static bool BeValidJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
