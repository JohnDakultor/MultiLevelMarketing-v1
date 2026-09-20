namespace modular_mlm.Application.Compensation.Commands.ProcessOutstandingPaidOrderCompensation;

public sealed record ProcessOutstandingPaidOrderCompensationCommand(int BatchSize = 50)
    : IRequest<int>;
