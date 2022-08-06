namespace WebScheduler.Server.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orleans;

/// <summary>
/// Verifies whether the <see cref="IStorageHealthCheckGrain"/> can read, write and clear state using the default
/// storage provider.
/// </summary>
public class StorageHealthCheck : IHealthCheck
{
    private const string FailedMessage = "Failed storage health check.";
    private readonly IClusterClient client;
    private readonly ILogger<StorageHealthCheck> logger;

    public StorageHealthCheck(IClusterClient client, ILogger<StorageHealthCheck> logger)
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
            // Call this grain with a random key each time. This grain then deactivates itself, so there is a new
            // instance created and destroyed each time.
            await this.client.GetGrain<IStorageHealthCheckGrain>(Guid.NewGuid()).CheckAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            this.logger.FailedStorageHealthCheck(exception);
            return HealthCheckResult.Unhealthy(FailedMessage, exception);
        }

        return HealthCheckResult.Healthy();
    }
}
