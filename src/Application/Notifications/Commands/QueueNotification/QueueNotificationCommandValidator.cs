using System.Text.RegularExpressions;

namespace modular_mlm.Application.Notifications.Commands.QueueNotification;

public sealed partial class QueueNotificationCommandValidator
    : AbstractValidator<QueueNotificationCommand>
{
    public QueueNotificationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.RecipientUserId).NotEmpty();
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.TemplateKey)
            .NotEmpty()
            .MaximumLength(100)
            .Must(NotificationTemplateCatalog.IsSupported)
            .WithMessage("Notification template is not supported.");
        RuleFor(command => command.Culture).NotEmpty().MaximumLength(20);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Variables)
            .NotNull()
            .Must(variables => variables.Count <= 32)
            .WithMessage("A notification cannot contain more than 32 variables.");
        RuleForEach(command => command.Variables)
            .Must(variable => IsSafeVariable(variable.Key, variable.Value))
            .WithMessage("Notification variables contain unsafe or oversized content.");
        RuleFor(command => command.ActionPath)
            .Must(IsSafeActionPath)
            .WithMessage("Action path must be a trusted relative application path.");
    }

    private static bool IsSafeVariable(string name, string value) =>
        VariableNameRegex().IsMatch(name)
        && value is not null
        && value.Length <= 1_000
        && !value.Contains('\r')
        && !value.Contains('\0')
        && !value.Contains("<script", StringComparison.OrdinalIgnoreCase)
        && !Uri.TryCreate(value, UriKind.Absolute, out _);

    private static bool IsSafeActionPath(string? actionPath) =>
        string.IsNullOrWhiteSpace(actionPath)
        || (
            actionPath.Length <= 2_048
            && actionPath.StartsWith('/')
            && !actionPath.StartsWith("//", StringComparison.Ordinal)
            && !Uri.TryCreate(actionPath, UriKind.Absolute, out _)
        );

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,49}$", RegexOptions.CultureInvariant)]
    private static partial Regex VariableNameRegex();
}
