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
    /// The temporary buffer of history records to move to <seealso cref="IHistoryGrain{ScheduledTaskMetadata, ScheduledTaskOperationType}"/> in a reliable way.
    /// </summary>
    public List<HistoryState<ScheduledTaskMetadata, ScheduledTaskOperationType>> HistoryBuffer { get; set; } = new();

    /// <summary>
    /// The temporary buffer of history records to move to <seealso cref="IHistoryGrain{ScheduledTaskTriggerHistory, TaskTriggerType}"/> in a reliable way.
    /// </summary>
    public List<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>> TriggerHistoryBuffer { get; set; } = new();

    /// <summary>
    /// This The owning tenantId.
    /// </summary>
    /// <remarks>
    /// This property is depricated in favor of <see cref="TenantIdString"/>.
    /// See https://github.com/web-scheduler/web-scheduler/issues/268.
    /// </remarks>
    [Obsolete("Use TenantIdString instead.", false, UrlFormat = "https://github.com/web-scheduler/web-scheduler/issues/268")]
    public Guid TenantId { get; set; }

    /// <summary>
    /// The owning tenantId.
    /// </summary>
    public string? TenantIdString { get; set; }

    /// <summary>
    /// Specifies if the task is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}
