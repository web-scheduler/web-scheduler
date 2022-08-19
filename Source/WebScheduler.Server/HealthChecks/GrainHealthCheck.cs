namespace WebScheduler.Server.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orleans;

/// <summary>
/// Verifies connectivity to a <see cref="ILocalHealthCheckGrain"/> activation. As this grain is a
/// stateless worker, validation always occurs in the silo where the health check is issued.
/// </summary>
public class GrainHealthCheck : IHealthCheck
{
    private const string FailedMessage = "Failed local health check.";
    private readonly IClusterClient client;
    private readonly ILogger<GrainHealthCheck> logger;

    public GrainHealthCheck(IClusterClient client, ILogger<GrainHealthCheck> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await this.client.GetGrain<ILocalHealthCheckGrain>(Guid.Empty.ToString()).CheckAsync();
        }
        catch (Exception exception)
        {
            this.logger.FailedLocalHealthCheck(exception);
            return HealthCheckResult.Unhealthy(FailedMessage, exception);
        }

        return HealthCheckResult.Healthy();
    }
}
