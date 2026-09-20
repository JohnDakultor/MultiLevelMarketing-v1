namespace modular_mlm.Application.Commerce.Commands.AddCartItem;

public sealed record AddCartItemCommand(Guid OrganizationId, Guid ProductVariantId, int Quantity)
    : IRequest<Guid>,
        ICustomerContextRequest;
