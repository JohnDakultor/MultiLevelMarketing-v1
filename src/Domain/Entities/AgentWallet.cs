using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Wallets;

public sealed class AgentWallet : OrganizationEntity
{
    private AgentWallet() { }

    public Guid AgentId { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public WalletStatus Status { get; private set; }

    public static AgentWallet Open(Guid organizationId, Guid agentId, string currency)
    {
        if (organizationId == Guid.Empty || agentId == Guid.Empty || currency.Length != 3)
            throw new DomainInvariantException(
                "Wallet organization, agent, and currency are required."
            );
        return new AgentWallet
        {
            OrganizationId = organizationId,
            AgentId = agentId,
            Currency = currency.ToUpperInvariant(),
            Status = WalletStatus.Active,
        };
    }

    public void Hold() => Status = WalletStatus.Held;

    public void Reactivate()
    {
        if (Status == WalletStatus.Closed)
            throw new DomainInvariantException("Closed wallet cannot be reactivated.");
        Status = WalletStatus.Active;
    }

    public void Close() => Status = WalletStatus.Closed;
}
