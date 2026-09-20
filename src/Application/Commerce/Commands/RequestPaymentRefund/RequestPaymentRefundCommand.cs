namespace modular_mlm.Application.Commerce.Commands.RequestPaymentRefund;

public sealed record RequestPaymentRefundCommand(
    Guid OrganizationId,
    Guid OrderId,
    decimal Amount,
    string Reason
) : IRequest<Guid>, IOrganizationAdminRequest;
