using modular_mlm.Application.Catalog.Models;

namespace modular_mlm.Application.Catalog.Commands.UploadProductImage;

public sealed class UploadProductImageCommandValidator
    : AbstractValidator<UploadProductImageCommand>
{
    public UploadProductImageCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(255);
        RuleFor(command => command.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(command => command.ContentLength)
            .InclusiveBetween(1, ProductImageLimits.MaximumContentLength);
        RuleFor(command => command.Content)
            .NotNull()
            .Must(content => content.CanRead)
            .WithMessage("Content stream must be readable.");
    }
}
