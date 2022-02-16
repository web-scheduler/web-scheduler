namespace WebScheduler.Abstractions.Grains.Scheduler;

using Orleans;

/// <summary>
/// Model for Scheduled Task metadata
/// </summary>
[GenerateSerializer]
public class ScheduledTaskMetadata
{
    /// <summary>
    /// Created timestamp.
    /// </summary>
    [Id(0)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [Id(1)]
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// The name of the scheduled task.
    /// </summary>
    [Id(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the scheduled task.
    /// </summary>
    [Id(3)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The next time the task will execute.
    /// </summary>
    [Id(4)]
    public DateTime? NextRunAt { get; set; }

    /// <summary>
    /// The next time the task will execute.
    /// </summary>
    [Id(5)]
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Specifies if the task is enabled.
    /// </summary>
    [Id(6)]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Specifies if the task is deleted.
    /// </summary>
    [Id(7)]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The time the scheduled task was deleted at.
    /// </summary>
    [Id(8)]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The Cron Expression
    /// </summary>
    [Id(9)]
    public string CronExpression { get; set; } = "* * * * *";
}
