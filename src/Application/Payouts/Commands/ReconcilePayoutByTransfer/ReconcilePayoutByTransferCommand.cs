namespace modular_mlm.Application.Payouts.Commands.ReconcilePayoutByTransfer;

public sealed record ReconcilePayoutByTransferCommand(string ProviderTransferId) : IRequest<bool>;
