namespace modular_mlm.Application.Commerce.Commands.HandlePaymentWebhook;

public sealed record HandlePaymentWebhookCommand(
    string EventId,
    string EventType,
    string CheckoutSessionId,
    string ReferenceNumber,
    string? ProviderPaymentId,
    DateTimeOffset OccurredAt,
    string PayloadHash
) : IRequest<bool>;
