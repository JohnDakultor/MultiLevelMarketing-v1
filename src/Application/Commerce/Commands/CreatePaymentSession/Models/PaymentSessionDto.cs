namespace modular_mlm.Application.Commerce.Commands.CreatePaymentSession.Models;

public sealed record PaymentSessionDto(
    Guid PaymentId,
    string ProviderCheckoutSessionId,
    Uri CheckoutUrl
);
