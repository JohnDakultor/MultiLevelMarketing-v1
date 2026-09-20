using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using modular_mlm.Infrastructure.Email;

namespace modular_mlm.Infrastructure.Monitoring;

public sealed class SmtpHealthCheck(IOptions<SmtpOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.Host) || value.Port is < 1 or > 65535)
            return HealthCheckResult.Unhealthy("SMTP host and port are not configured.");
        if (string.IsNullOrWhiteSpace(value.FromAddress))
            return HealthCheckResult.Unhealthy("SMTP sender address is not configured.");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var client = new TcpClient();
            await client.ConnectAsync(value.Host, value.Port, timeout.Token);
            return HealthCheckResult.Healthy("SMTP endpoint is reachable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("SMTP readiness probe timed out.");
        }
        catch (SocketException exception)
        {
            return HealthCheckResult.Unhealthy("SMTP endpoint is unreachable.", exception);
        }
    }
}
