namespace modular_mlm.Domain.Events;

public sealed record WalletFundsReservedEvent(
    Guid OrganizationId,
    Guid WalletId,
    Guid PayoutRequestId,
    decimal Amount,
    string Currency
) : BaseEvent;
