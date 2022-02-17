namespace WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// Model for Scheduled Task metadata
/// </summary>
public class ScheduledTaskMetadata
{
    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// The name of the scheduled task.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the scheduled task.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The next time the task will execute.
    /// </summary>
    public DateTime? NextRunAt { get; set; }

    /// <summary>
    /// The next time the task will execute.
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Specifies if the task is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Specifies if the task is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The time the scheduled task was deleted at.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The Cron Expression
    /// </summary>
    public string CronExpression { get; set; } = "* * * * *";

    /// <summary>
    /// The trigger type for the task.
    /// </summary>
    public TaskTriggerType TriggerType { get; set; }

    /// <summary>
    /// The properties for the task.
    /// </summary>
    public Dictionary<string, object> TriggerProperties { get; set; } = new();
}
