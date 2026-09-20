namespace modular_mlm.Application.Catalog.Commands.PublishProduct;

public sealed class PublishProductCommandValidator : AbstractValidator<PublishProductCommand>
{
    public PublishProductCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();
    }
}
