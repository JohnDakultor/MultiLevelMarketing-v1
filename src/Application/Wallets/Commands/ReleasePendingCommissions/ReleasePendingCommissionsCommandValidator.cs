namespace modular_mlm.Application.Wallets.Commands.ReleasePendingCommissions;

public sealed class ReleasePendingCommissionsCommandValidator
    : AbstractValidator<ReleasePendingCommissionsCommand>
{
    public const int MaximumBatchSize = 500;

    public ReleasePendingCommissionsCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.BatchSize).InclusiveBetween(1, MaximumBatchSize);
        RuleFor(command => command.CutoffTime)
            .Must(cutoff => cutoff is null || cutoff.Value.Offset == TimeSpan.Zero)
            .WithMessage("Cutoff time must use the UTC offset.");
    }
}
