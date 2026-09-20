using modular_mlm.Application.Organizations.Models;

namespace modular_mlm.Application.Organizations.Commands.UploadBrandingAsset;

public sealed class UploadBrandingAssetCommandValidator
    : AbstractValidator<UploadBrandingAssetCommand>
{
    public UploadBrandingAssetCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AssetKind).IsInEnum();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(255);
        RuleFor(command => command.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(command => command.ContentLength)
            .InclusiveBetween(1, BrandingAssetLimits.MaximumContentLength);
        RuleFor(command => command.Content)
            .NotNull()
            .Must(content => content.CanRead)
            .WithMessage("Content stream must be readable.");
    }
}
