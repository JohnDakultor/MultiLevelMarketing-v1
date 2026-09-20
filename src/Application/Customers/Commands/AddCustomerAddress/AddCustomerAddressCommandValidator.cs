using modular_mlm.Domain.Identity;

namespace modular_mlm.Application.Customers.Commands.AddCustomerAddress;

public sealed class AddCustomerAddressCommandValidator
    : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.Label)
            .NotEmpty()
            .MaximumLength(CustomerAddress.MaximumLabelLength);
        RuleFor(command => command.RecipientName)
            .NotEmpty()
            .MaximumLength(CustomerAddress.MaximumRecipientNameLength);
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .MaximumLength(CustomerAddress.MaximumPhoneNumberLength)
            .Matches(@"^\+?[0-9][0-9\s().-]{5,31}$");
        RuleFor(command => command.AddressLine1)
            .NotEmpty()
            .MaximumLength(CustomerAddress.MaximumAddressLineLength);
        RuleFor(command => command.AddressLine2)
            .MaximumLength(CustomerAddress.MaximumAddressLineLength);
        RuleFor(command => command.Barangay).MaximumLength(CustomerAddress.MaximumBarangayLength);
        RuleFor(command => command.CityOrMunicipality)
            .NotEmpty()
            .MaximumLength(CustomerAddress.MaximumLocalityLength);
        RuleFor(command => command.Province)
            .NotEmpty()
            .MaximumLength(CustomerAddress.MaximumLocalityLength);
        RuleFor(command => command.PostalCode)
            .NotEmpty()
            .MaximumLength(CustomerAddress.MaximumPostalCodeLength);
        RuleFor(command => command.CountryCode).NotEmpty().Matches("^[A-Za-z]{2,3}$");
    }
}
