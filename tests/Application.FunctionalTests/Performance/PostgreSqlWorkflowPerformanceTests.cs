using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Auditing.Queries.GetAuditTrail;
using modular_mlm.Application.Commerce.Commands.AddCartItem;
using modular_mlm.Application.Commerce.Commands.CreateCheckout;
using modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;
using modular_mlm.Application.Network.Commands.PlaceAgent;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payments;
using modular_mlm.Domain.Wallets;
using modular_mlm.Infrastructure.BackgroundJobs;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Application.FunctionalTests.Performance;

using static Infrastructure.TestApp;

[Explicit("Run intentionally against PostgreSQL; timings depend on the local environment.")]
public sealed class PostgreSqlWorkflowPerformanceTests : TestBase
{
    [Test]
    public async Task CatalogQueryP95()
    {
        var organization = Organization.Create(
            "Perf Catalog",
            $"perf-catalog-{Guid.NewGuid():N}",
            "PHP"
        );
        var category = Category.Create(organization.Id, "Products", $"products-{Guid.NewGuid():N}");
        var products = Enumerable
            .Range(0, 100)
            .Select(index =>
            {
                var product = Product.Create(
                    organization.Id,
                    category.Id,
                    $"Product {index}",
                    $"product-{index}-{Guid.NewGuid():N}",
                    "Performance product"
                );
                product.AddVariant($"SKU-{index}-{Guid.NewGuid():N}", 100m, 10m, 100);
                product.Publish();
                return product;
            })
            .ToArray();
        await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            db.AddRange(organization, category);
            db.AddRange(products);
            return await db.SaveChangesAsync();
        });

        var durations = await MeasureAsync(
            30,
            () =>
                ExecuteInScopeAsync(async services =>
                    await services
                        .GetRequiredService<ApplicationDbContext>()
                        .Products.AsNoTracking()
                        .Where(x =>
                            x.OrganizationId == organization.Id && x.Status == ProductStatus.Active
                        )
                        .OrderBy(x => x.Name)
                        .Take(20)
                        .ToListAsync()
                )
        );

        Report("catalog-query", durations);
    }

    [Test]
    public async Task PlacementWrite()
    {
        var organization = Organization.Create(
            "Perf Placement",
            $"perf-placement-{Guid.NewGuid():N}",
            "PHP"
        );
        var parent = Agent.Apply(
            organization.Id,
            "parent",
            "PERF-P",
            "PERF-RP",
            DateTimeOffset.UtcNow
        );
        parent.Activate(DateTimeOffset.UtcNow);
        var child = Agent.Apply(
            organization.Id,
            "child",
            "PERF-C",
            "PERF-RC",
            DateTimeOffset.UtcNow
        );
        await AddAsync(organization);
        await AddAsync(parent);
        await AddAsync(child);
        await RunAsAdministratorAsync(organization.Id);

        var duration = await MeasureOneAsync(() =>
            SendAsync(
                new PlaceAgentCommand(organization.Id, child.Id, parent.Id, PlacementSide.Left)
            )
        );

        Report("placement-write", [duration]);
    }

    [Test]
    public async Task CheckoutServerProcessing()
    {
        var organization = Organization.Create(
            "Perf Checkout",
            $"perf-checkout-{Guid.NewGuid():N}",
            "PHP"
        );
        var userId = await RunAsUserAsync(
            $"perf-checkout-{Guid.NewGuid():N}@local",
            "Testing1234!",
            []
        );
        var customer = CustomerProfile.Create(organization.Id, userId, "Performance Customer");
        var category = Category.Create(
            organization.Id,
            "Products",
            $"checkout-products-{Guid.NewGuid():N}"
        );
        var product = Product.Create(
            organization.Id,
            category.Id,
            "Checkout Product",
            $"checkout-product-{Guid.NewGuid():N}",
            "Performance product"
        );
        var variant = product.AddVariant($"SKU-{Guid.NewGuid():N}", 100m, 10m, 100);
        product.Publish();
        await AddAsync(organization);
        await AddAsync(customer);
        await AddAsync(category);
        await AddAsync(product);
        await SendAsync(new AddCartItemCommand(organization.Id, variant.Id, 1));
        var address = new CheckoutAddressInput(
            "Customer",
            "+639171234567",
            "1 Test Street",
            null,
            null,
            "Manila",
            "Metro Manila",
            "1000",
            "PH"
        );

        var duration = await MeasureOneAsync(() =>
            SendAsync(new CreateCheckoutCommand(organization.Id, address, address))
        );

        Report("checkout-processing", [duration]);
    }

    [Test]
    public async Task AuditQueryP95()
    {
        var organization = Organization.Create(
            "Perf Audit",
            $"perf-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var userId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            db.AuditLogs.AddRange(
                Enumerable
                    .Range(0, 1_000)
                    .Select(index =>
                        AuditLog.Record(
                            organization.Id,
                            userId,
                            "PERF",
                            "Order",
                            Guid.NewGuid(),
                            null,
                            "{}",
                            $"event {index}",
                            "127.0.0.1",
                            "PerformanceTests",
                            null
                        )
                    )
            );
            return await db.SaveChangesAsync();
        });

        var durations = await MeasureAsync(
            30,
            () => SendAsync(new GetAuditTrailQuery(organization.Id, PageSize: 50))
        );

        Report("audit-query", durations);
    }

    [Test]
    public async Task RefundReversal()
    {
        var (organization, order, item, payment) =
            await Commerce.RequestItemRefundTests.CreatePaidOrderAsync();
        await RunAsAdministratorAsync(organization.Id);
        var now = DateTimeOffset.UtcNow;
        var providerRefund = PaymentRefund.Request(
            organization.Id,
            payment.Id,
            order.Id,
            100m,
            "PHP",
            "performance",
            now
        );
        providerRefund.AttachProviderResult("perf_refund", "succeeded", now);
        var itemRefund = OrderItemRefund.Create(
            organization.Id,
            order.Id,
            item.Id,
            providerRefund.Id,
            1m,
            100m,
            75m,
            20m,
            4m
        );
        await AddAsync(providerRefund);
        await AddAsync(itemRefund);

        var duration = await MeasureOneAsync(() =>
            SendAsync(new ProcessItemRefundReversalCommand(organization.Id, itemRefund.Id))
        );

        Report("refund-reversal", [duration]);
    }

    [Test]
    public async Task OutboxBatch()
    {
        await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            db.OutboxMessages.AddRange(
                Enumerable
                    .Range(0, 100)
                    .Select(_ =>
                        OutboxMessage.Create(
                            new OrderRefundedEvent(Guid.NewGuid()),
                            DateTimeOffset.UtcNow,
                            null
                        )
                    )
            );
            return await db.SaveChangesAsync();
        });

        var duration = await MeasureOneAsync(() =>
            ExecuteInScopeAsync(services =>
                services.GetRequiredService<OutboxDispatcher>().DispatchBatchAsync(100, default)
            )
        );

        Report("outbox-batch-100", [duration]);
    }

    [Test]
    public async Task LargeBinaryPairingBatch()
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "Perf Pairing",
            $"perf-pairing-{Guid.NewGuid():N}",
            "PHP"
        );
        var plan = CommissionPlan.Draft(organization.Id, "Performance Binary", 1, now.AddDays(-30));
        plan.ConfigureBinaryPairing(
            BinaryPairingRule.Percentage(0.10m, ProcessingFrequency.Weekly)
        );
        plan.Publish();
        var agents = Enumerable
            .Range(0, 100)
            .Select(index =>
            {
                var agent = Agent.Apply(
                    organization.Id,
                    Guid.NewGuid().ToString(),
                    $"PA-{index}-{Guid.NewGuid():N}",
                    $"PR-{index}-{Guid.NewGuid():N}",
                    now.AddDays(-30)
                );
                agent.Activate(now.AddDays(-29));
                return agent;
            })
            .ToArray();
        var balances = agents
            .Select(agent =>
            {
                var balance = BinaryVolumeBalance.Open(organization.Id, agent.Id);
                balance.Credit(PlacementSide.Left, 1_000m);
                balance.Credit(PlacementSide.Right, 1_000m);
                return balance;
            })
            .ToArray();
        await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            db.AddRange(organization, plan);
            db.AddRange(agents);
            db.AddRange(balances);
            return await db.SaveChangesAsync();
        });

        var duration = await MeasureOneAsync(() =>
            ExecuteInScopeAsync(services =>
                services
                    .GetRequiredService<BinaryPairingJob>()
                    .ExecuteAsync(organization.Id, plan.Id, now.AddDays(-7), now, default)
            )
        );

        Report("binary-pairing-100", [duration]);
    }

    private static async Task<double[]> MeasureAsync(int samples, Func<Task> operation)
    {
        var values = new double[samples];
        for (var index = 0; index < samples; index++)
            values[index] = await MeasureOneAsync(operation);
        return values;
    }

    private static async Task<double> MeasureOneAsync(Func<Task> operation)
    {
        var started = Stopwatch.GetTimestamp();
        await operation();
        return Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    private static void Report(string operation, IReadOnlyCollection<double> durations)
    {
        var ordered = durations.Order().ToArray();
        var p95 = ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
        TestContext.Progress.WriteLine(
            $"{operation}: samples={ordered.Length}, p50={ordered[ordered.Length / 2]:F2}ms, p95={p95:F2}ms, max={ordered[^1]:F2}ms"
        );
        p95.ShouldBeLessThan(30_000d);
    }
}
