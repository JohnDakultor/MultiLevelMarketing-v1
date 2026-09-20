using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Reporting.Models;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Payouts;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Reporting.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryHandler(
    IApplicationDbContext db,
    IOperationalHealthReader healthReader,
    TimeProvider clock
) : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    public async Task<AdminDashboardDto> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken
    )
    {
        var now = clock.GetUtcNow();
        var organization =
            await db
                .Organizations.AsNoTracking()
                .Where(x => x.Id == request.OrganizationId)
                .Select(x => new { x.CurrencyCode, x.TimeZone })
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Organization was not found.");
        var today = StartOfLocalDay(now, organization.TimeZone);
        var paidOrders = db
            .Orders.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.PaymentStatus != PaymentStatus.Pending
                && x.PaymentStatus != PaymentStatus.Failed
            );
        var walletIds = db
            .AgentWallets.Where(x => x.OrganizationId == request.OrganizationId)
            .Select(x => x.Id);
        var pendingApplications = await db.Agents.CountAsync(
            x =>
                x.OrganizationId == request.OrganizationId
                && (x.Status == AgentStatus.Applied || x.Status == AgentStatus.PendingApproval),
            cancellationToken
        );
        var payoutReviews = await db.PayoutRequests.CountAsync(
            x =>
                x.OrganizationId == request.OrganizationId
                && (x.Status == PayoutStatus.Requested || x.Status == PayoutStatus.UnderReview),
            cancellationToken
        );
        var refundReviews = await db.OrderItemRefunds.CountAsync(
            x =>
                x.OrganizationId == request.OrganizationId
                && x.Status == OrderItemRefundStatus.Pending,
            cancellationToken
        );
        var productIds = db
            .Products.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId && x.Status == ProductStatus.Active
            )
            .Select(x => x.Id);
        var lowStock = await db.ProductVariants.CountAsync(
            x =>
                productIds.Contains(x.ProductId)
                && x.StockKeepingEnabled
                && x.Status == ProductStatus.Active
                && x.StockQuantity - x.ReservedQuantity <= 0,
            cancellationToken
        );
        var health = await healthReader.ReadAsync(request.OrganizationId, now, cancellationToken);
        var tasks = new List<DashboardTaskDto>
        {
            new(
                "agent_applications",
                "Agent applications awaiting review",
                pendingApplications,
                "warning"
            ),
            new("payout_reviews", "Payouts awaiting review", payoutReviews, "warning"),
            new("refund_reviews", "Refunds awaiting processing", refundReviews, "warning"),
            new("out_of_stock", "Tracked variants out of stock", lowStock, "critical"),
        }
            .Where(x => x.Count > 0)
            .ToArray();
        var alerts = new List<DashboardAlertDto>();
        if (health.PendingOutboxMessages > 0)
            alerts.Add(
                new(
                    "outbox_backlog",
                    $"{health.PendingOutboxMessages} messages await dispatch.",
                    "warning"
                )
            );
        if (health.FailedWebhookAttempts > 0)
            alerts.Add(
                new(
                    "webhook_failures",
                    $"{health.FailedWebhookAttempts} webhook attempts failed.",
                    "critical"
                )
            );
        if (health.CompensationBacklog > 0)
            alerts.Add(
                new(
                    "compensation_backlog",
                    $"{health.CompensationBacklog} paid orders await compensation.",
                    "critical"
                )
            );

        return new AdminDashboardDto(
            request.OrganizationId,
            now,
            organization.CurrencyCode,
            await paidOrders
                .Where(x => x.PaidAt >= today)
                .SumAsync(x => (decimal?)x.GrandTotal, cancellationToken)
                ?? 0m,
            await paidOrders
                .Where(x => x.PaidAt >= now.AddDays(-30))
                .SumAsync(x => (decimal?)x.GrandTotal, cancellationToken)
                ?? 0m,
            await paidOrders.CountAsync(x => x.PaidAt >= today, cancellationToken),
            await db.Agents.CountAsync(
                x => x.OrganizationId == request.OrganizationId && x.Status == AgentStatus.Active,
                cancellationToken
            ),
            await db
                .CommissionTransactions.Where(x =>
                    x.OrganizationId == request.OrganizationId
                    && (
                        x.Status == CommissionStatus.Pending
                        || x.Status == CommissionStatus.Available
                        || x.Status == CommissionStatus.Held
                    )
                )
                .SumAsync(x => (decimal?)x.Amount, cancellationToken)
                ?? 0m,
            await db
                .WalletEntries.Where(x =>
                    walletIds.Contains(x.WalletId) && x.Type != WalletEntryType.PendingCredit
                )
                .SumAsync(x => (decimal?)x.Amount, cancellationToken)
                ?? 0m,
            tasks,
            alerts
        );
    }

    private static DateTimeOffset StartOfLocalDay(DateTimeOffset now, string timeZoneId)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = TimeZoneInfo.Utc;
        }
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var localMidnight = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMidnight, timeZone));
    }
}
