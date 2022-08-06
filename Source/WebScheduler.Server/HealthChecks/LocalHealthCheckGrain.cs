namespace WebScheduler.Server.HealthChecks;

using Orleans;
using Orleans.Concurrency;
using WebScheduler.Server.HealthChecks;

[StatelessWorker(1)]
public class LocalHealthCheckGrain : Grain, ILocalHealthCheckGrain
{
    public ValueTask CheckAsync() => ValueTask.CompletedTask;
}
