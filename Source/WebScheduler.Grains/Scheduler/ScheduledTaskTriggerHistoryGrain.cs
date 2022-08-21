namespace WebScheduler.Grains.Scheduler;

using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Grains.Constants;
using Orleans.Placement;
using WebScheduler.Grains.History;
using WebScheduler.Abstractions.Services;

/// <summary>
/// Records <see cref="ScheduledTaskTriggerHistoryGrain"/> history.
/// </summary>
[PreferLocalPlacement]
public class ScheduledTaskTriggerHistoryGrain : HistoryGrain<ScheduledTaskTriggerHistory, TaskTriggerType>, IScheduledTaskTriggerHistoryGrain
{
    /// <summary>
    /// The ctor.
    /// </summary>
    /// <param name="exceptionObserver"></param>
    /// <param name="logger">logger</param>
    /// <param name="state">state</param>
    public ScheduledTaskTriggerHistoryGrain(
        IExceptionObserver exceptionObserver,
        ILogger<ScheduledTaskTriggerHistoryGrain> logger,
        [PersistentState(StateName.ScheduledTaskTriggerHistory, GrainStorageProviderName.ScheduledTaskTriggerHistory)]
        IPersistentState<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>> state) : base(exceptionObserver, logger, state)
    { }
}
