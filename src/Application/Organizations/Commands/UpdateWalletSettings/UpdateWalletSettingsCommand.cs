using Domain.Enums;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Organizations.Commands.UpdateWalletSettings;

public sealed record UpdateWalletSettingsCommand(
    Guid OrganizationId,
    CommissionReleaseTrigger CommissionReleaseTrigger,
    int ReleaseDelayDays,
    int ReturnWindowDays,
    decimal MinimumPayoutAmount,
    bool AllowNegativeRecoverableBalance,
    decimal MaximumNegativeBalance
) : IRequest, IOrganizationAdminRequest;
