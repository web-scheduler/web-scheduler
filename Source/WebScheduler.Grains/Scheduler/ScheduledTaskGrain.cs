namespace WebScheduler.Grains.Scheduler;

using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using WebScheduler.Abstractions.Constants;
using WebScheduler.Abstractions.Grains;

public class ScheduledTaskGrain : Grain, IScheduledTaskGrain
{
    private readonly ILogger<ScheduledTaskGrain> logger;
    private readonly IClusterClient clusterClient;
    private readonly IPersistentState<ScheduledTaskMetadata> scheduledTaskMetadata;

    public ScheduledTaskGrain(ILogger<ScheduledTaskGrain> logger, IClusterClient clusterClient, [PersistentState(StateName.ScheduledTaskMetadata, GrainStorageProviderName.ScheduledTaskMetadata)] IPersistentState<ScheduledTaskMetadata> scheduledTaskDefinition)
    {
        this.logger = logger;
        this.clusterClient = clusterClient;
        this.scheduledTaskMetadata = scheduledTaskDefinition;
    }

    /// <summary>
    /// Creates a new scheduled task.
    /// </summary>
    /// <param name="scheduledTaskMetadata"></param>
    /// <returns><value>true</value> if created, <value>false if it already exists.</value></returns>
    public async ValueTask<bool> CreateAsync(ScheduledTaskMetadata scheduledTaskMetadata)
    {
        if (this.scheduledTaskMetadata.RecordExists)
        {
            this.logger.ScheduledTaskAlreadyExists(this.GetPrimaryKeyString());
            return false;
        }
        this.scheduledTaskMetadata.State = scheduledTaskMetadata;
        await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(true);
        return true;
    }

    /// <summary>
    /// Returns the scheduled task.
    /// </summary>
    /// <returns>The scheduled task metadata.</returns>
    public ValueTask<ScheduledTaskMetadata?> GetAsync()
    {
        if (!this.scheduledTaskMetadata.RecordExists)
        {
            this.logger.ScheduledTaskDoesNotExists(this.GetPrimaryKeyString());
            return new ValueTask<ScheduledTaskMetadata?>();
        }

        return new ValueTask<ScheduledTaskMetadata?>(this.scheduledTaskMetadata.State);
    }
}
