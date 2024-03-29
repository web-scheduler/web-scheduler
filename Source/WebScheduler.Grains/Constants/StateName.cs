namespace WebScheduler.Grains.Constants;

/// <summary>
/// Grain state names.
/// </summary>
public static class StateName
{
    /// <summary>
    /// The constant for <see cref="Abstractions.Grains.Scheduler.ScheduledTaskMetadata"/> Grain State.
    /// </summary>
    public const string ScheduledTaskState = nameof(ScheduledTaskState);

    /// <summary>
    /// The constant for <see cref="Abstractions.Grains.Scheduler.ScheduledTaskMetadata"/> History Grain State.
    /// </summary>
    public const string ScheduledTaskMetadataHistory = nameof(ScheduledTaskMetadataHistory);

    /// <summary>
    /// The constant for <see cref="ScheduledTaskTriggerHistory"/> History Grain State.
    /// </summary>
    public const string ScheduledTaskTriggerHistory = nameof(ScheduledTaskTriggerHistory);
}
