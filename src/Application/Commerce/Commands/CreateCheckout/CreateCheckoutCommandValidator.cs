namespace modular_mlm.Application.Commerce.Commands.CreateCheckout;

public sealed class CreateCheckoutCommandValidator : AbstractValidator<CreateCheckoutCommand>
{
    public CreateCheckoutCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ShippingAddress)
            .NotNull()
            .SetValidator(new CheckoutAddressInputValidator());
        RuleFor(command => command.BillingAddress)
            .NotNull()
            .SetValidator(new CheckoutAddressInputValidator());
    }
}

public sealed class CheckoutAddressInputValidator : AbstractValidator<CheckoutAddressInput>
{
    public CheckoutAddressInputValidator()
    {
        RuleFor(address => address.RecipientName).NotEmpty();
        RuleFor(address => address.PhoneNumber).NotEmpty();
        RuleFor(address => address.AddressLine1).NotEmpty();
        RuleFor(address => address.CityOrMunicipality).NotEmpty();
        RuleFor(address => address.Province).NotEmpty();
        RuleFor(address => address.PostalCode).NotEmpty();
        RuleFor(address => address.CountryCode).NotEmpty().Length(2);
    }
}
