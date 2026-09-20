namespace modular_mlm.Application.Commerce.Commands.RemoveCartItem;

public sealed record RemoveCartItemCommand(Guid OrganizationId, Guid CartItemId)
    : IRequest<CartDto>,
        ICustomerContextRequest;
