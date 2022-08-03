namespace WebScheduler.Abstractions.Grains.HealthChecks;

using Orleans;
using Orleans.CodeGeneration;

/// <summary>
/// A local health check grain
/// </summary>
[Version(1)]
public interface ILocalHealthCheckGrain : IGrainWithGuidKey
{
    /// <summary>
    ///  checks the health.
    /// </summary>
    /// <returns>A value task.</returns>
    ValueTask CheckAsync();
}
