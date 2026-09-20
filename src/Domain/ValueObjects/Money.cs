namespace modular_mlm.Domain.ValueObjects;

public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException(
                "Currency must be a three-letter ISO code.",
                nameof(currency)
            );
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }
    public string Currency { get; }
}
