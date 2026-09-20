using Microsoft.AspNetCore.DataProtection;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.Payouts;

public sealed class PayoutAccountProtector(IDataProtectionProvider provider)
    : IPayoutAccountProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector(
        "modular_mlm.PayoutAccount.v1"
    );

    public string Protect(string value) => _protector.Protect(value);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
