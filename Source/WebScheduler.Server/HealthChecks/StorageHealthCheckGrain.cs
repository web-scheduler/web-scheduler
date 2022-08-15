namespace WebScheduler.Server.HealthChecks;

using Orleans;
using Orleans.Placement;
using Orleans.Runtime;

[PreferLocalPlacement]
public class StorageHealthCheckGrain : Grain<string>, IStorageHealthCheckGrain
{
    public async ValueTask CheckAsync()
    {
        try
        {
            this.State = Guid.NewGuid().ToString();
            await this.WriteStateAsync();
            await this.ReadStateAsync();
            await this.ClearStateAsync(); // This does not actually remove the state from ADO Grain State Provider.
        }
        finally
        {
            this.DeactivateOnIdle();
        }
    }
}
