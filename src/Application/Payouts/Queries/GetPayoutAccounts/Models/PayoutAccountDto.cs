using modular_mlm.Domain.Payouts;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutAccounts.Models;

public sealed record PayoutAccountDto(
    Guid Id,
    string Method,
    string MaskedAccountData,
    string BankCode,
    string Rail,
    PayoutVerificationStatus VerificationStatus,
    bool IsDefault
);
