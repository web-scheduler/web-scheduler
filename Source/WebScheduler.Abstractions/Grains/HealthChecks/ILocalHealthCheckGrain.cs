namespace WebScheduler.Abstractions.Grains.HealthChecks;

using Orleans;

/// <summary>
/// A local health check grain
/// </summary>
public interface ILocalHealthCheckGrain : IGrainWithGuidKey
{
    /// <summary>
    ///  checks the health.
    /// </summary>
    /// <returns>A value task.</returns>
    ValueTask CheckAsync();
}
