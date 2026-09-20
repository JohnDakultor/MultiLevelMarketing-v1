namespace modular_mlm.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandValidator
    : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.NotificationId).NotEmpty();
    }
}
