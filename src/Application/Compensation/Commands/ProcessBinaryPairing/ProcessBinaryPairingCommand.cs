namespace modular_mlm.Application.Compensation.Commands.ProcessBinaryPairing;

public sealed record ProcessBinaryPairingCommand(
    Guid OrganizationId,
    Guid AgentId,
    Guid CommissionPlanId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    string IdempotencyKey
) : IRequest<Guid>;
