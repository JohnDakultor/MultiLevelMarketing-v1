using Domain.Enums;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Wallets.Commands.CreateWalletAdjustment;

public sealed record CreateWalletAdjustmentCommand(
    Guid OrganizationId,
    Guid AgentId,
    WalletAdjustmentDirection Direction,
    decimal Amount,
    string Currency,
    string Reason,
    string IdempotencyKey
) : IRequest<Guid>, IOrganizationAdminRequest;
