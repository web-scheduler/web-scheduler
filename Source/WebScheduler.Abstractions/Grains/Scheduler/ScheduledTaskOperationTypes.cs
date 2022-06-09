namespace WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// The available operations to record.
/// </summary>
public enum ScheduledTaskOperationType
{
    /// <summary>
    /// The unset value. Using this will cause an error.
    /// </summary>
    Unset = 0,

    /// <summary>
    /// Represents a Create event.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Represents an Update event.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Represents a Delete event.
    /// </summary>
    Delete = 3
}
