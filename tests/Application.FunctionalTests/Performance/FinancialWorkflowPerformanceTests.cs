using System.Diagnostics;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.FunctionalTests.Performance;

public sealed class FinancialWorkflowPerformanceTests
{
    [Test]
    [Explicit("Run intentionally as a local performance smoke test; timing is machine-dependent.")]
    public void ShouldCreateTenThousandProportionalReversalsWithinASmokeTestBudget()
    {
        const int transactionCount = 10_000;
        var organizationId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var sources = Enumerable
            .Range(0, transactionCount)
            .Select(index =>
                CommissionTransaction.CreateDirectSale(
                    organizationId,
                    agentId,
                    orderId,
                    itemId,
                    planId,
                    $"performance-{index}",
                    100m,
                    0.10m,
                    10m
                )
            )
            .ToArray();
        var stopwatch = Stopwatch.StartNew();

        var reversals = sources
            .Select(source => source.ReverseForRefund(Guid.NewGuid(), 25m, 2.50m, false))
            .ToArray();

        stopwatch.Stop();
        reversals.Sum(entry => entry.Amount).ShouldBe(-25_000m);
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
        TestContext.Progress.WriteLine(
            $"Created {transactionCount:N0} linked reversals in {stopwatch.Elapsed.TotalMilliseconds:N2} ms."
        );
    }
}
