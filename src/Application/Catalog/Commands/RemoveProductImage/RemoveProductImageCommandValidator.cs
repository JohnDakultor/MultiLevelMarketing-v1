namespace modular_mlm.Application.Catalog.Commands.RemoveProductImage;

public sealed class RemoveProductImageCommandValidator
    : AbstractValidator<RemoveProductImageCommand>
{
    public RemoveProductImageCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();
    }
}
