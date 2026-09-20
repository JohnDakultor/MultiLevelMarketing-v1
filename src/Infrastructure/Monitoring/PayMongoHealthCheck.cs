using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using modular_mlm.Infrastructure.Payments;

namespace modular_mlm.Infrastructure.Monitoring;

public sealed class PayMongoHealthCheck(
    IHttpClientFactory clientFactory,
    IOptions<PayMongoOptions> options
) : IHealthCheck
{
    public const string ClientName = "PayMongoHealth";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var configuration = options.Value;
        if (string.IsNullOrWhiteSpace(configuration.SecretKey))
            return HealthCheckResult.Unhealthy("PayMongo secret key is not configured.");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var request = new HttpRequestMessage(HttpMethod.Get, "v1/payments/health-check");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration.SecretKey}:"))
            );
            using var response = await clientFactory
                .CreateClient(ClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            return
                response.IsSuccessStatusCode
                || response.StatusCode == System.Net.HttpStatusCode.NotFound
                ? HealthCheckResult.Healthy("PayMongo API is reachable.")
                : HealthCheckResult.Unhealthy(
                    $"PayMongo API returned HTTP {(int)response.StatusCode}."
                );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("PayMongo API health request timed out.");
        }
        catch (HttpRequestException exception)
        {
            return HealthCheckResult.Unhealthy("PayMongo API is unreachable.", exception);
        }
    }
}
