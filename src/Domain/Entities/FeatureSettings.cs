namespace modular_mlm.Domain.Organizations;

public sealed class FeatureSettings
{
    private FeatureSettings() { }

    public bool CommerceEnabled { get; private set; }
    public bool AgentProgramEnabled { get; private set; }
    public bool BinaryNetworkEnabled { get; private set; }
    public bool BinaryPairingEnabled { get; private set; }
    public bool WalletEnabled { get; private set; }
    public bool PayoutEnabled { get; private set; }
    public bool ReviewsEnabled { get; private set; }
    public bool CouponsEnabled { get; private set; }

    public static FeatureSettings Default() =>
        new()
        {
            CommerceEnabled = true,
            AgentProgramEnabled = true,
            BinaryNetworkEnabled = true,
            BinaryPairingEnabled = true,
            WalletEnabled = true,
            PayoutEnabled = true,
        };

    public void SetCommerce(bool enabled) => CommerceEnabled = enabled;

    public void SetAgentProgram(bool enabled)
    {
        AgentProgramEnabled = enabled;
        if (!enabled)
        {
            BinaryNetworkEnabled = false;
            BinaryPairingEnabled = false;
        }
    }

    public void SetBinaryNetwork(bool enabled)
    {
        BinaryNetworkEnabled = enabled;
        if (!enabled)
            BinaryPairingEnabled = false;
    }

    public void SetBinaryPairing(bool enabled)
    {
        if (enabled && !BinaryNetworkEnabled)
            throw new InvalidOperationException("Binary network must be enabled first.");
        BinaryPairingEnabled = enabled;
    }

    public void SetWallet(bool enabled)
    {
        WalletEnabled = enabled;
        if (!enabled)
            PayoutEnabled = false;
    }

    public void SetPayout(bool enabled)
    {
        if (enabled && !WalletEnabled)
            throw new InvalidOperationException("Wallet must be enabled first.");
        PayoutEnabled = enabled;
    }
}
