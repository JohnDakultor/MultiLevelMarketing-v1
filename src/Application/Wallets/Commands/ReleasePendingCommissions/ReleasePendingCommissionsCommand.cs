using modular_mlm.Application.Wallets.Commands.ReleasePendingCommissions.Models;

namespace modular_mlm.Application.Wallets.Commands.ReleasePendingCommissions;

public sealed record ReleasePendingCommissionsCommand(
    Guid OrganizationId,
    int BatchSize = 100,
    DateTimeOffset? CutoffTime = null
) : IRequest<ReleasePendingCommissionsResult>;
