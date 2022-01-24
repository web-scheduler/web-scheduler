namespace WebScheduler.Abstractions.Grains.HealthChecks;

using Orleans;

/// <summary>
/// A storage healthcheck grain
/// </summary>
public interface IStorageHealthCheckGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Checks the health of storage for grains.
    /// </summary>
    /// <returns>A value task.</returns>
    ValueTask CheckAsync();
}
