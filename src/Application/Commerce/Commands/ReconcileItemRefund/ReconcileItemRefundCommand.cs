namespace modular_mlm.Application.Commerce.Commands.ReconcileItemRefund;

public sealed record ReconcileItemRefundCommand(Guid OrganizationId, Guid OrderItemRefundId)
    : IRequest<bool>,
        IOrganizationAdminRequest;
