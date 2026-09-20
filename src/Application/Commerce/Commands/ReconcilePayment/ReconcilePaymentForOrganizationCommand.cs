namespace modular_mlm.Application.Commerce.Commands.ReconcilePayment;

public sealed record ReconcilePaymentForOrganizationCommand(Guid OrganizationId, Guid PaymentId)
    : IRequest<bool>,
        IOrganizationAdminRequest;
