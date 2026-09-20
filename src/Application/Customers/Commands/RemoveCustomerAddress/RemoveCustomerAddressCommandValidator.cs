namespace modular_mlm.Application.Customers.Commands.RemoveCustomerAddress;

public sealed class RemoveCustomerAddressCommandValidator
    : AbstractValidator<RemoveCustomerAddressCommand>
{
    public RemoveCustomerAddressCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AddressId).NotEmpty();
    }
}
