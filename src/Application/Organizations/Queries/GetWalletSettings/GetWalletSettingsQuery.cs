using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Queries.GetWalletSettings.Models;

namespace modular_mlm.Application.Organizations.Queries.GetWalletSettings;

public sealed record GetWalletSettingsQuery(Guid OrganizationId)
    : IRequest<WalletSettingsDto?>,
        IOrganizationAdminRequest;
