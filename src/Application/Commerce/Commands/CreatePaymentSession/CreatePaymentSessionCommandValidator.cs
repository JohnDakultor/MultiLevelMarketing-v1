namespace modular_mlm.Application.Commerce.Commands.CreatePaymentSession;

public sealed class CreatePaymentSessionCommandValidator
    : AbstractValidator<CreatePaymentSessionCommand>
{
    private static readonly string[] SupportedMethods =
    [
        "card",
        "gcash",
        "paymaya",
        "grab_pay",
        "shopeepay",
        "qrph",
    ];

    public CreatePaymentSessionCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.SuccessUrl).Must(IsHttpUrl);
        RuleFor(command => command.CancelUrl).Must(IsHttpUrl);
        RuleFor(command => command.PaymentMethodTypes).NotEmpty();
        RuleForEach(command => command.PaymentMethodTypes)
            .Must(method => SupportedMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
            .WithMessage("An unsupported PayMongo payment method was requested.");
    }

    private static bool IsHttpUrl(Uri uri) =>
        uri.IsAbsoluteUri
        && (
            string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
        );
}
