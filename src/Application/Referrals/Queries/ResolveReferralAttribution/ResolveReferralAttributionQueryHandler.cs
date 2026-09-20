using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Referrals.Queries.ResolveReferralAttribution.Models;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Referrals.Queries.ResolveReferralAttribution;

public sealed class ResolveReferralAttributionQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ResolveReferralAttributionQuery, ReferralAttributionDto?>
{
    public async Task<ReferralAttributionDto?> Handle(
        ResolveReferralAttributionQuery request,
        CancellationToken cancellationToken
    )
    {
        var normalizedCode = request.ReferralCode.Trim().ToUpperInvariant();

        return await db
            .Agents.AsNoTracking()
            .Where(agent =>
                agent.OrganizationId == request.OrganizationId
                && agent.ReferralCode == normalizedCode
                && agent.Status == AgentStatus.Active
            )
            .Select(agent => new ReferralAttributionDto(
                agent.OrganizationId,
                agent.Id,
                agent.AgentCode,
                agent.ReferralCode
            ))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
