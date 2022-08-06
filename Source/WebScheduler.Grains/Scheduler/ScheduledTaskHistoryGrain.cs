namespace WebScheduler.Grains.Scheduler;

using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Grains.Constants;
using WebScheduler.Grains.History;

/// <summary>
/// History for <see cref="IScheduledTaskGrain"/>.
/// </summary>
public class ScheduledTaskHistoryGrain : HistoryGrain<ScheduledTaskMetadata, ScheduledTaskOperationType>
{
    /// <summary>
    /// The ctor.
    /// </summary>
    /// <param name="logger">logger</param>
    /// <param name="state">state</param>
    public ScheduledTaskHistoryGrain(
        ILogger<ScheduledTaskHistoryGrain> logger,
        [PersistentState(StateName.ScheduledTaskMetadataHistory, GrainStorageProviderName.ScheduledTaskMetadataHistory)]
        IPersistentState<HistoryState<ScheduledTaskMetadata,ScheduledTaskOperationType>> state) : base(logger, state)
    { }
}
