using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Payouts;

public sealed class PayoutAccount : OrganizationEntity
{
    private PayoutAccount() { }

    public Guid AgentId { get; private set; }
    public string Method { get; private set; } = string.Empty;
    public string MaskedAccountData { get; private set; } = string.Empty;
    public string ProtectedAccountName { get; private set; } = string.Empty;
    public string ProtectedAccountNumber { get; private set; } = string.Empty;
    public string BankCode { get; private set; } = string.Empty;
    public string Rail { get; private set; } = string.Empty;
    public PayoutVerificationStatus VerificationStatus { get; private set; }
    public bool IsDefault { get; private set; }

    public static PayoutAccount Register(
        Guid organizationId,
        Guid agentId,
        string method,
        string maskedData
    )
    {
        if (
            organizationId == Guid.Empty
            || agentId == Guid.Empty
            || string.IsNullOrWhiteSpace(method)
        )
            throw new DomainInvariantException("Payout account values are required.");
        return new PayoutAccount
        {
            OrganizationId = organizationId,
            AgentId = agentId,
            Method = method,
            MaskedAccountData = maskedData,
            VerificationStatus = PayoutVerificationStatus.Unverified,
        };
    }

    public static PayoutAccount Register(
        Guid organizationId,
        Guid agentId,
        string method,
        string maskedData,
        string protectedAccountName,
        string protectedAccountNumber,
        string bankCode,
        string rail
    )
    {
        var account = Register(organizationId, agentId, method, maskedData);
        if (
            string.IsNullOrWhiteSpace(protectedAccountName)
            || string.IsNullOrWhiteSpace(protectedAccountNumber)
            || string.IsNullOrWhiteSpace(bankCode)
            || rail is not ("instapay" or "pesonet")
        )
            throw new DomainInvariantException("Provider payout account details are invalid.");
        account.ProtectedAccountName = protectedAccountName;
        account.ProtectedAccountNumber = protectedAccountNumber;
        account.BankCode = bankCode.Trim().ToUpperInvariant();
        account.Rail = rail;
        return account;
    }

    public void SubmitForVerification()
    {
        if (VerificationStatus != PayoutVerificationStatus.Unverified)
            throw new DomainInvariantException("Account cannot be submitted again.");
        VerificationStatus = PayoutVerificationStatus.Pending;
    }

    public void Verify() => VerificationStatus = PayoutVerificationStatus.Verified;

    public void Reject() => VerificationStatus = PayoutVerificationStatus.Rejected;

    public void MakeDefault()
    {
        if (VerificationStatus != PayoutVerificationStatus.Verified)
            throw new DomainInvariantException("Only a verified account can be default.");
        IsDefault = true;
    }

    public void RemoveDefault() => IsDefault = false;
}
