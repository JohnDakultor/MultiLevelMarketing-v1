using modular_mlm.Application.Commerce.Commands.AddCartItem;

namespace modular_mlm.Application.Commerce.Commands.UpdateCartItem;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.CartItemId).NotEmpty();
        RuleFor(command => command.Quantity)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(modular_mlm.Domain.Commerce.Cart.MaximumQuantityPerLine);
    }
}
