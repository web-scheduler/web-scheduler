namespace WebScheduler.Grains.Scheduler;

using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Grains.Constants;
using Orleans.Placement;
using WebScheduler.Grains.History;
using WebScheduler.Abstractions.Services;
using Orleans.StorageProviderInterceptors.Abstractions;

/// <summary>
/// History for <see cref="IScheduledTaskGrain"/>.
/// </summary>
[PreferLocalPlacement]
public class ScheduledTaskHistoryGrain : HistoryGrain<ScheduledTaskMetadata, ScheduledTaskOperationType>, IScheduledTaskHistoryGrain
{
    /// <summary>
    /// The ctor.
    /// </summary>
    /// <param name="logger">logger</param>
    /// <param name="exceptionObserver"></param>
    /// <param name="state">state</param>
    public ScheduledTaskHistoryGrain(
        ILogger<ScheduledTaskHistoryGrain> logger,
        IExceptionObserver exceptionObserver,
        [StorageInterceptor(StateName.ScheduledTaskMetadataHistory, GrainStorageProviderName.ScheduledTaskMetadataHistory)]
        IPersistentState<HistoryState<ScheduledTaskMetadata,ScheduledTaskOperationType>> state) : base(exceptionObserver, logger, state)
    { }
}
