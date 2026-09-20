namespace modular_mlm.Application.Catalog.Commands.CreateProductCommissionProfile;

public sealed class CreateProductCommissionProfileCommandValidator
    : AbstractValidator<CreateProductCommissionProfileCommand>
{
    public CreateProductCommissionProfileCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.DirectSalesRateOverride)
            .InclusiveBetween(0m, 1m)
            .When(command => command.DirectSalesRateOverride.HasValue);
        RuleFor(command => command.DirectSalesRateOverride)
            .Null()
            .When(command => !command.DirectSalesEligible);
        RuleFor(command => command.BinaryVolumeOverride)
            .GreaterThanOrEqualTo(0m)
            .When(command => command.BinaryVolumeOverride.HasValue);
        RuleFor(command => command.BinaryVolumeOverride)
            .Null()
            .When(command => !command.BinaryVolumeEligible);
    }
}
