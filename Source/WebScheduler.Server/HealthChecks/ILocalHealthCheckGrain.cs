namespace WebScheduler.Server.HealthChecks;

using Orleans;
using Orleans.CodeGeneration;

/// <summary>
/// A local health check grain
/// </summary>
[Version(1)]
public interface ILocalHealthCheckGrain : IGrainWithStringKey
{
    ValueTask CheckAsync();
}
