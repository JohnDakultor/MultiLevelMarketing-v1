using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Reporting.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.Reporting.Queries.GetAgentReport;

public sealed class GetAgentReportQueryHandler(IApplicationDbContext db, IUser user)
    : IRequestHandler<GetAgentReportQuery, AgentReportDto>
{
    public async Task<AgentReportDto> Handle(
        GetAgentReportQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        var context =
            await db
                .Agents.AsNoTracking()
                .Where(x => x.OrganizationId == request.OrganizationId && x.UserId == userId)
                .Join(
                    db.Organizations.AsNoTracking(),
                    agent => agent.OrganizationId,
                    organization => organization.Id,
                    (agent, organization) => new { AgentId = agent.Id, organization.CurrencyCode }
                )
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Current Agent profile was not found.");

        var orders = db
            .Orders.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.AttributedAgentId == context.AgentId
                && x.PaidAt >= request.From
                && x.PaidAt < request.To
                && x.PaymentStatus != PaymentStatus.Pending
                && x.PaymentStatus != PaymentStatus.Failed
            );
        var orderIds = orders.Select(x => x.Id);
        var gross = await orders.SumAsync(x => (decimal?)x.GrandTotal, cancellationToken) ?? 0m;
        var refunded =
            await db
                .PaymentRefunds.AsNoTracking()
                .Where(x =>
                    orderIds.Contains(x.OrderId) && x.Status == PaymentRefundStatus.Succeeded
                )
                .SumAsync(x => (decimal?)x.Amount, cancellationToken)
            ?? 0m;
        var commissions = await db
            .CommissionTransactions.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.BeneficiaryAgentId == context.AgentId
                && x.Created >= request.From
                && x.Created < request.To
            )
            .GroupBy(x => x.Type)
            .Select(x => new CommissionTypeAmountDto(x.Key, x.Count(), x.Sum(y => y.Amount)))
            .ToListAsync(cancellationToken);
        var payouts = await db
            .PayoutRequests.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.AgentId == context.AgentId
                && x.RequestedAt >= request.From
                && x.RequestedAt < request.To
            )
            .GroupBy(x => x.Status)
            .Select(x => new StatusAmountDto(x.Key.ToString(), x.Count(), x.Sum(y => y.Amount)))
            .ToListAsync(cancellationToken);

        return new AgentReportDto(
            request.OrganizationId,
            context.AgentId,
            request.From,
            request.To,
            context.CurrencyCode,
            await orders.CountAsync(cancellationToken),
            gross,
            refunded,
            gross - refunded,
            await db.Agents.CountAsync(
                x =>
                    x.OrganizationId == request.OrganizationId
                    && x.SponsorAgentId == context.AgentId,
                cancellationToken
            ),
            await db.PlacementClosures.CountAsync(
                x =>
                    x.OrganizationId == request.OrganizationId
                    && x.AncestorAgentId == context.AgentId
                    && x.Depth > 0,
                cancellationToken
            ),
            await db
                .BinaryVolumeEntries.Where(x =>
                    x.OrganizationId == request.OrganizationId
                    && x.OwnerAgentId == context.AgentId
                    && x.EffectiveAt >= request.From
                    && x.EffectiveAt < request.To
                )
                .SumAsync(x => (decimal?)x.Volume, cancellationToken)
                ?? 0m,
            commissions,
            commissions.Sum(x => x.Amount),
            payouts
        );
    }
}
