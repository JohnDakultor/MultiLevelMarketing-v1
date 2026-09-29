using modular_mlm.Domain.Payouts;

namespace modular_mlm.Application.Payouts.Queries.GetAdminPayoutAccounts.Models;

public sealed record AdminPayoutAccountDto(
    Guid Id,
    Guid AgentId,
    string AgentCode,
    string Method,
    string MaskedAccountData,
    string BankCode,
    string Rail,
    PayoutVerificationStatus VerificationStatus,
    bool IsDefault,
    DateTimeOffset CreatedAt
);
