namespace WebScheduler.Grains.Scheduler;

using WebScheduler.Grains.History;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Grains.Constants;

/// <summary>
/// Records <see cref="ScheduledTaskTriggerHistoryGrain"/> history.
/// </summary>
public class ScheduledTaskTriggerHistoryGrain : HistoryGrain<ScheduledTaskTriggerHistory, TaskTriggerType>
{
    /// <summary>
    /// The ctor.
    /// </summary>
    /// <param name="logger">logger</param>
    /// <param name="state">state</param>
    public ScheduledTaskTriggerHistoryGrain(
        ILogger<ScheduledTaskHistoryGrain> logger,
        [PersistentState(StateName.ScheduledTaskTriggerHistory, GrainStorageProviderName.ScheduledTaskTriggerHistory)]
        IPersistentState<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>> state) : base(logger, state)
    { }
}
