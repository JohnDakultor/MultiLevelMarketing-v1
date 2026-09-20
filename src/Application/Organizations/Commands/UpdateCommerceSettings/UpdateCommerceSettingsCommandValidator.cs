using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UpdateCommerceSettings;

public sealed class UpdateCommerceSettingsCommandValidator
    : AbstractValidator<UpdateCommerceSettingsCommand>
{
    public UpdateCommerceSettingsCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.InventoryReservationMinutes)
            .InclusiveBetween(
                CommerceSettings.MinimumInventoryReservationMinutes,
                CommerceSettings.MaximumInventoryReservationMinutes
            );
    }
}
