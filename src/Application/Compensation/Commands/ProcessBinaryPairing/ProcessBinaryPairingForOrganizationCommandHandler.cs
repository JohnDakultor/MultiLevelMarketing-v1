namespace modular_mlm.Application.Compensation.Commands.ProcessBinaryPairing;

public sealed class ProcessBinaryPairingForOrganizationCommandHandler(ISender sender)
    : IRequestHandler<ProcessBinaryPairingForOrganizationCommand, Guid>
{
    public Task<Guid> Handle(
        ProcessBinaryPairingForOrganizationCommand request,
        CancellationToken cancellationToken
    ) =>
        sender.Send(
            new ProcessBinaryPairingCommand(
                request.OrganizationId,
                request.AgentId,
                request.CommissionPlanId,
                request.PeriodStart,
                request.PeriodEnd,
                request.IdempotencyKey
            ),
            cancellationToken
        );
}
