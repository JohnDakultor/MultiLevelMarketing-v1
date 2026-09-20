using modular_mlm.Domain.Identity;

namespace modular_mlm.Application.Customers.Commands.UpdateCurrentCustomerProfile;

public sealed class UpdateCurrentCustomerProfileCommandValidator
    : AbstractValidator<UpdateCurrentCustomerProfileCommand>
{
    public UpdateCurrentCustomerProfileCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MaximumLength(CustomerProfile.MaximumDisplayNameLength);
    }
}
