namespace WebScheduler.Abstractions.Constants;
/// <summary>
/// Grain state names.
/// </summary>
public static class StateName
{
    /// <summary>
    /// The constant for <see cref="Grains.Scheduler.ScheduledTaskMetadata"/> Grain State.
    /// </summary>
    public const string ScheduledTaskState = nameof(ScheduledTaskState);

    /// <summary>
    /// The constant for <see cref="Grains.Scheduler.ScheduledTaskMetadata"/> History Grain State.
    /// </summary>
    public const string ScheduledTaskMetadataHistory = nameof(ScheduledTaskMetadataHistory);
}
