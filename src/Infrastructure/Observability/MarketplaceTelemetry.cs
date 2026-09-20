using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace modular_mlm.Infrastructure.Observability;

public static class MarketplaceTelemetry
{
    public const string ActivitySourceName = "modular_mlm.marketplace";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(ActivitySourceName);
    public static readonly Counter<long> OutboxMessagesProcessed = Meter.CreateCounter<long>(
        "marketplace.outbox.messages.processed"
    );
    public static readonly Histogram<double> OutboxDispatchDuration = Meter.CreateHistogram<double>(
        "marketplace.outbox.dispatch.duration",
        "ms"
    );
    public static readonly Counter<long> BackgroundJobFailures = Meter.CreateCounter<long>(
        "marketplace.background_job.failures"
    );
    public static readonly Counter<long> WebhookRetries = Meter.CreateCounter<long>(
        "marketplace.webhook.retries"
    );
    public static readonly Counter<long> RateLimitRejections = Meter.CreateCounter<long>(
        "marketplace.http.rate_limit.rejections"
    );
    public static readonly Counter<long> OrdersPaid = Meter.CreateCounter<long>(
        "marketplace.orders.paid"
    );
    public static readonly Counter<long> PaymentSuccesses = Meter.CreateCounter<long>(
        "marketplace.payments.succeeded"
    );
    public static readonly Counter<long> PaymentFailures = Meter.CreateCounter<long>(
        "marketplace.payments.failed"
    );
    public static readonly Counter<long> PayoutFailures = Meter.CreateCounter<long>(
        "marketplace.payouts.failed"
    );
    public static readonly Counter<long> PlacementConflicts = Meter.CreateCounter<long>(
        "marketplace.placements.conflicts"
    );
    public static readonly Histogram<double> ApiRequestDuration = Meter.CreateHistogram<double>(
        "marketplace.api.request.duration",
        "ms"
    );
    public static readonly Histogram<double> OutboxLag = Meter.CreateHistogram<double>(
        "marketplace.outbox.lag",
        "s"
    );
    public static readonly Histogram<double> CompensationLag = Meter.CreateHistogram<double>(
        "marketplace.compensation.lag",
        "s"
    );
    public static readonly Histogram<double> BinaryPairingDuration = Meter.CreateHistogram<double>(
        "marketplace.binary_pairing.duration",
        "ms"
    );
    public static readonly Counter<long> BinaryPairingFailures = Meter.CreateCounter<long>(
        "marketplace.binary_pairing.failures"
    );
    public static readonly Counter<long> CommissionReleaseOrganizationsProcessed =
        Meter.CreateCounter<long>("marketplace.commission_release.organizations.processed");
    public static readonly Counter<long> CommissionsReleased = Meter.CreateCounter<long>(
        "marketplace.commission_release.commissions.released"
    );
    public static readonly Histogram<double> CommissionReleaseDuration =
        Meter.CreateHistogram<double>("marketplace.commission_release.duration", "ms");
    public static readonly Histogram<double> CommissionReleaseLag = Meter.CreateHistogram<double>(
        "marketplace.commission_release.lag",
        "s"
    );
}
