namespace WebScheduler.Grains.Scheduler;

using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Grains.Constants;
using WebScheduler.Grains.History;

public class ScheduledTaskHistoryGrain : HistoryGrain<ScheduledTaskMetadata, ScheduledTaskOperationType>
{
    public ScheduledTaskHistoryGrain(
        ILogger<ScheduledTaskHistoryGrain> logger,
        [PersistentState(StateName.ScheduledTaskMetadataHistory, GrainStorageProviderName.ScheduledTaskMetadataHistory)]
        IPersistentState<HistoryState<ScheduledTaskMetadata,ScheduledTaskOperationType>> scheduledTaskDefinition) : base(logger, scheduledTaskDefinition)
    { }
}
