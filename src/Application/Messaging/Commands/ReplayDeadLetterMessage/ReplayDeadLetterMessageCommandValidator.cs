namespace modular_mlm.Application.Messaging.Commands.ReplayDeadLetterMessage;

public sealed class ReplayDeadLetterMessageCommandValidator
    : AbstractValidator<ReplayDeadLetterMessageCommand>
{
    private const int MaximumOutboxAttempts = 8;

    public ReplayDeadLetterMessageCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.MessageId).NotEmpty();
        RuleFor(command => command.ExpectedAttempts).GreaterThanOrEqualTo(MaximumOutboxAttempts);
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1_000);
    }
}
