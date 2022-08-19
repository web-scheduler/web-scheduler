namespace WebScheduler.Server.HealthChecks;

using Orleans;
using Orleans.CodeGeneration;

/// <summary>
/// A storage healthcheck grain
/// </summary>
[Version(1)]
public interface IStorageHealthCheckGrain : IGrainWithStringKey
{
    ValueTask CheckAsync();
}
