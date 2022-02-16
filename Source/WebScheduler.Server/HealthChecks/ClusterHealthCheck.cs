namespace WebScheduler.Server.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orleans;
using Orleans.Runtime;

/// <summary>
/// Verifies whether any silos are unavailable by querying the <see cref="IManagementGrain"/>.
/// </summary>
public class ClusterHealthCheck : IHealthCheck
{
    private const string DegradedMessage = " silo(s) unavailable.";
    private const string FailedMessage = "Failed cluster status health check.";
    private readonly IClusterClient client;
    private readonly ILogger<ClusterHealthCheck> logger;

    public ClusterHealthCheck(IClusterClient client, ILogger<ClusterHealthCheck> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var manager = this.client.GetGrain<IManagementGrain>(0);

        try
        {
            var hosts = await manager.GetHosts().ConfigureAwait(false);
            var count = hosts.Values.Count(x => x.IsTerminating() || x == SiloStatus.None);
            return count > 0 ? HealthCheckResult.Degraded(count + DegradedMessage) : HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            this.logger.FailedClusterStatusHealthCheck(exception);
            return HealthCheckResult.Unhealthy(FailedMessage, exception);
        }
    }
}
