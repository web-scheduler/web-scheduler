namespace WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// Model for Scheduled Task metadata
/// </summary>
public class ScheduledTaskMetadata
{
    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTimeOffset Modified { get; set; }

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
    public TimeSpan? NextRunAt { get; set; }

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
    public DateTimeOffset DeletedAt { get; set; }

    /// <summary>
    /// The cron schedule.
    /// </summary>
    public string CronExpression { get; set; } = "* * * * *"; // Every Minute
}
