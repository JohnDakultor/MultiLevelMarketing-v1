namespace modular_mlm.Application.Compensation.Commands.ProcessBinaryPairing;

public sealed record ProcessBinaryPairingForOrganizationCommand(
    Guid OrganizationId,
    Guid AgentId,
    Guid CommissionPlanId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    string IdempotencyKey
) : IRequest<Guid>, IOrganizationAdminRequest;
