namespace WebScheduler.Abstractions.Grains.HealthChecks;

using Orleans;
using Orleans.CodeGeneration;

/// <summary>
/// A storage healthcheck grain
/// </summary>
[Version(1)]
public interface IStorageHealthCheckGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Checks the health of storage for grains.
    /// </summary>
    /// <returns>A value task.</returns>
    ValueTask CheckAsync();
}
