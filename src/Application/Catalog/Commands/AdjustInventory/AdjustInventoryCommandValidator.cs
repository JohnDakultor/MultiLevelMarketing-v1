using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Commands.AdjustInventory;

public sealed class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    private const int MaximumAbsoluteAdjustment = 1_000_000;

    public AdjustInventoryCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ProductVariantId).NotEmpty();
        RuleFor(command => command.QuantityDelta)
            .NotEqual(0)
            .InclusiveBetween(-MaximumAbsoluteAdjustment, MaximumAbsoluteAdjustment);
        RuleFor(command => command.AdjustmentType).IsInEnum();
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(InventoryAdjustment.MaximumReasonLength);
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(InventoryAdjustment.MaximumIdempotencyKeyLength);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0);

        RuleFor(command => command)
            .Must(HaveValidDirection)
            .WithMessage("The quantity direction does not match the inventory adjustment type.");
    }

    private static bool HaveValidDirection(AdjustInventoryCommand command) =>
        command.AdjustmentType switch
        {
            InventoryAdjustmentType.Receipt
            or InventoryAdjustmentType.CorrectionIncrease
            or InventoryAdjustmentType.Return => command.QuantityDelta > 0,
            InventoryAdjustmentType.CorrectionDecrease or InventoryAdjustmentType.Damage =>
                command.QuantityDelta < 0,
            _ => false,
        };
}
