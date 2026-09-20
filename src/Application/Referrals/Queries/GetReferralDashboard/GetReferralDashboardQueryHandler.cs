using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Referrals.Queries.GetReferralDashboard.Models;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Referrals.Queries.GetReferralDashboard;

public sealed class GetReferralDashboardQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReferralDashboardQuery, ReferralDashboardDto>
{
    public async Task<ReferralDashboardDto> Handle(
        GetReferralDashboardQuery request,
        CancellationToken cancellationToken
    )
    {
        var agent = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (agent is null)
            throw new KeyNotFoundException("Agent was not found.");

        var directRecruitCount = await db.Agents.CountAsync(
            x => x.OrganizationId == request.OrganizationId && x.SponsorAgentId == request.AgentId,
            cancellationToken
        );
        var attributedOrders = db
            .Orders.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId && x.AttributedAgentId == request.AgentId
            );
        var orderCount = await attributedOrders.CountAsync(cancellationToken);
        var attributedSales =
            await attributedOrders.SumAsync(x => (decimal?)x.GrandTotal, cancellationToken) ?? 0;
        var commissions = db
            .CommissionTransactions.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.BeneficiaryAgentId == request.AgentId
            );
        var pending =
            await commissions
                .Where(x =>
                    x.Status == CommissionStatus.Pending || x.Status == CommissionStatus.Held
                )
                .SumAsync(x => (decimal?)x.Amount, cancellationToken)
            ?? 0;
        var available =
            await commissions
                .Where(x => x.Status == CommissionStatus.Available)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken)
            ?? 0;

        return new ReferralDashboardDto(
            agent.Id,
            agent.ReferralCode,
            $"/r/{Uri.EscapeDataString(agent.ReferralCode)}",
            directRecruitCount,
            orderCount,
            attributedSales,
            pending,
            available
        );
    }
}
