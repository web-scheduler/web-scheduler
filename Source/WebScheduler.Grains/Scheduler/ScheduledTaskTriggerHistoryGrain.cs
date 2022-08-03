namespace WebScheduler.Grains.Scheduler;

using WebScheduler.Grains.History;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Grains.Constants;

public class ScheduledTaskTriggerHistoryGrain : HistoryGrain<ScheduledTaskTriggerHistory, TaskTriggerType>
{
    public ScheduledTaskTriggerHistoryGrain(
        ILogger<ScheduledTaskHistoryGrain> logger,
        [PersistentState(StateName.ScheduledTaskTriggerHistory, GrainStorageProviderName.ScheduledTaskTriggerHistory)]
        IPersistentState<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>> scheduledTaskTriggerHistory) : base(logger, scheduledTaskTriggerHistory)
    { }
}
