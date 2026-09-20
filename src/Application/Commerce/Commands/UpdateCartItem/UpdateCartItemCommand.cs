namespace modular_mlm.Application.Commerce.Commands.UpdateCartItem;

public sealed record UpdateCartItemCommand(Guid OrganizationId, Guid CartItemId, int Quantity)
    : IRequest<CartDto>,
        ICustomerContextRequest;
