namespace modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;

public sealed record ProcessItemRefundReversalCommand(Guid OrganizationId, Guid OrderItemRefundId)
    : IRequest<bool>;
