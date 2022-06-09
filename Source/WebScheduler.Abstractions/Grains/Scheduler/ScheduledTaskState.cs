namespace WebScheduler.Abstractions.Grains.Scheduler;

using WebScheduler.Abstractions.Grains.History;

/// <summary>
/// The state for the <see cref="IScheduledTaskGrain"/>.
/// </summary>
public class ScheduledTaskState
{
    /// <summary>
    /// The Task definition.
    /// </summary>
    public ScheduledTaskMetadata Task { get; set; } = new();

    /// <summary>
    /// The temporary buffer of history records to move to <seealso cref="IHistoryGrain{TGrainState, TOperationTypes}"/> in a reliable way.
    /// </summary>
    public List<HistoryState<ScheduledTaskMetadata, ScheduledTaskOperationType>> HistoryBuffer { get; set; } = new();

    /// <summary>
    /// The owning tenantId.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Specifies if the task is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}
