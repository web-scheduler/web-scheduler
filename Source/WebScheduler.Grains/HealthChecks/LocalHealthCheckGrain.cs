namespace WebScheduler.Grains;

using Orleans;
using Orleans.Concurrency;
using WebScheduler.Abstractions.Grains.HealthChecks;

[StatelessWorker(1)]
public class LocalHealthCheckGrain : Grain, ILocalHealthCheckGrain
{
    public ValueTask CheckAsync() => ValueTask.CompletedTask;
}
