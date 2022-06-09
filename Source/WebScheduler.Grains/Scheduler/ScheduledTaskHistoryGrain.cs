namespace WebScheduler.Grains.History;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using WebScheduler.Abstractions.Constants;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;

public class ScheduledTaskHistoryGrain : Grain, IHistoryGrain<ScheduledTaskMetadata, ScheduledTaskOperationType>
{
    private readonly ILogger<ScheduledTaskHistoryGrain> logger;
    private readonly IPersistentState<HistoryState<ScheduledTaskMetadata, ScheduledTaskOperationType>> historyRecordState;

    public ScheduledTaskHistoryGrain(ILogger<ScheduledTaskHistoryGrain> logger,
    [PersistentState(StateName.ScheduledTaskMetadataHistory, GrainStorageProviderName.ScheduledTaskMetadataHistory)]
    IPersistentState<HistoryState<ScheduledTaskMetadata, ScheduledTaskOperationType>> scheduledTaskDefinition)
    {
        this.logger = logger;
        this.historyRecordState = scheduledTaskDefinition;
    }

    public async ValueTask<bool> RecordAsync(HistoryState<ScheduledTaskMetadata, ScheduledTaskOperationType> history)
    {
        // If we've already recorded the data, it is a retry from a partially failed operation, so discard it.
        if (this.historyRecordState.RecordExists)
        {
            return true;
        }

        try
        {
            this.historyRecordState.State = history;
            await this.historyRecordState.WriteStateAsync().ConfigureAwait(true);
            this.DeactivateOnIdle();
            return true;
        }
        catch (Exception ex)
        {
            this.logger.ErrorRecordingHistory(ex, this.GetPrimaryKeyString());
            return false;
        }
    }
}
