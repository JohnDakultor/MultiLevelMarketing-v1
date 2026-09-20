using modular_mlm.Application.Organizations.Queries.GetReferralSettings.Models;

namespace modular_mlm.Application.Organizations.Queries.GetReferralSettings;

public sealed record GetReferralSettingsQuery(Guid OrganizationId) : IRequest<ReferralSettingsDto>;
