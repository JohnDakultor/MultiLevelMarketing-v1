using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Reporting.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Payments;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Reporting.Queries.GetAdminReport;

public sealed class GetAdminReportQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminReportQuery, AdminReportDto>
{
    public async Task<AdminReportDto> Handle(
        GetAdminReportQuery request,
        CancellationToken cancellationToken
    )
    {
        var organization =
            await db
                .Organizations.AsNoTracking()
                .Where(x => x.Id == request.OrganizationId)
                .Select(x => new { x.CurrencyCode })
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Organization was not found.");

        var orders = db
            .Orders.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.PaidAt >= request.From
                && x.PaidAt < request.To
                && x.PaymentStatus != PaymentStatus.Pending
                && x.PaymentStatus != PaymentStatus.Failed
            );
        var orderIds = orders.Select(x => x.Id);
        var grossSales =
            await orders.SumAsync(x => (decimal?)x.GrandTotal, cancellationToken) ?? 0m;
        var orderCount = await orders.CountAsync(cancellationToken);
        var refunds =
            await db
                .PaymentRefunds.AsNoTracking()
                .Where(x =>
                    x.OrganizationId == request.OrganizationId
                    && x.Status == PaymentRefundStatus.Succeeded
                    && x.CompletedAt >= request.From
                    && x.CompletedAt < request.To
                )
                .SumAsync(x => (decimal?)x.Amount, cancellationToken)
            ?? 0m;

        var productRows = await db
            .OrderItems.AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .GroupBy(x => new { x.ProductId, x.ProductNameSnapshot })
            .Select(x => new
            {
                x.Key.ProductId,
                x.Key.ProductNameSnapshot,
                Orders = x.Select(item => item.OrderId).Distinct().Count(),
                GrossSales = x.Sum(item => item.LineTotal),
            })
            .OrderByDescending(x => x.GrossSales)
            .Take(20)
            .ToListAsync(cancellationToken);
        var salesByProduct = productRows
            .Select(x => new SalesBreakdownDto(
                x.ProductId,
                x.ProductNameSnapshot,
                x.Orders,
                x.GrossSales
            ))
            .ToArray();
        var agentRows = await orders
            .Where(x => x.AttributedAgentId != null)
            .Join(
                db.Agents.AsNoTracking(),
                order => order.AttributedAgentId,
                agent => agent.Id,
                (order, agent) => new { order, agent }
            )
            .GroupBy(x => new { x.agent.Id, x.agent.AgentCode })
            .Select(x => new
            {
                x.Key.Id,
                x.Key.AgentCode,
                Orders = x.Count(),
                GrossSales = x.Sum(row => row.order.GrandTotal),
            })
            .OrderByDescending(x => x.GrossSales)
            .Take(20)
            .ToListAsync(cancellationToken);
        var salesByAgent = agentRows
            .Select(x => new SalesBreakdownDto(x.Id, x.AgentCode, x.Orders, x.GrossSales))
            .ToArray();

        var commissions = db
            .CommissionTransactions.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.Created >= request.From
                && x.Created < request.To
            );
        var walletIds = db
            .AgentWallets.AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId)
            .Select(x => x.Id);
        var payouts = await db
            .PayoutRequests.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.RequestedAt >= request.From
                && x.RequestedAt < request.To
            )
            .GroupBy(x => x.Status)
            .Select(x => new StatusAmountDto(x.Key.ToString(), x.Count(), x.Sum(y => y.Amount)))
            .ToListAsync(cancellationToken);

        return new AdminReportDto(
            request.OrganizationId,
            request.From,
            request.To,
            organization.CurrencyCode,
            orderCount,
            grossSales,
            refunds,
            grossSales - refunds,
            salesByProduct,
            salesByAgent,
            await db.Agents.CountAsync(
                x =>
                    x.OrganizationId == request.OrganizationId
                    && x.JoinedAt >= request.From
                    && x.JoinedAt < request.To,
                cancellationToken
            ),
            await db.Agents.CountAsync(
                x => x.OrganizationId == request.OrganizationId && x.Status == AgentStatus.Active,
                cancellationToken
            ),
            await db
                .BinaryVolumeEntries.Where(x =>
                    x.OrganizationId == request.OrganizationId
                    && x.EffectiveAt >= request.From
                    && x.EffectiveAt < request.To
                )
                .SumAsync(x => (decimal?)x.Volume, cancellationToken)
                ?? 0m,
            await commissions.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            await commissions
                .Where(x =>
                    x.Status == CommissionStatus.Pending
                    || x.Status == CommissionStatus.Available
                    || x.Status == CommissionStatus.Held
                )
                .SumAsync(x => (decimal?)x.Amount, cancellationToken)
                ?? 0m,
            await db
                .WalletEntries.Where(x =>
                    walletIds.Contains(x.WalletId) && x.Type != WalletEntryType.PendingCredit
                )
                .SumAsync(x => (decimal?)x.Amount, cancellationToken)
                ?? 0m,
            payouts
        );
    }
}
