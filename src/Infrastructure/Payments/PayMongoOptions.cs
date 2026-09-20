namespace modular_mlm.Infrastructure.Payments;

public sealed class PayMongoOptions
{
    public const string SectionName = "PayMongo";

    public Uri BaseUrl { get; init; } = new("https://api.paymongo.com/");
    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public int WebhookToleranceMinutes { get; init; } = 5;
    public string WalletAccountNumber { get; init; } = string.Empty;
    public string WalletAccountName { get; init; } = string.Empty;
    public string WalletBic { get; init; } = "PAEYPHM2XXX";
    public Uri PayoutCallbackUrl { get; init; } =
        new("https://localhost:7179/api/webhooks/paymongo/transfers");
}
