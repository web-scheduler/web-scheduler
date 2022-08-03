namespace WebScheduler.Grains.Constants;

/// <summary>
/// Constants for Grain Storage provider Names.
/// </summary>
public static class GrainStorageProviderName
{
    /// <summary>
    /// The storage provider name for <see cref="ScheduledTaskState "/>.
    /// </summary>
    public const string ScheduledTaskState = nameof(ScheduledTaskState);

    /// <summary>
    /// The storage provider name for <see cref="ScheduledTaskHistoryGrain"/>.
    /// </summary>
    public const string ScheduledTaskMetadataHistory = nameof(ScheduledTaskMetadataHistory);

    /// <summary>
    /// The storage provider name for <see cref="ScheduledTaskTriggerHistory "/>
    /// </summary>
    public const string ScheduledTaskTriggerHistory = nameof(ScheduledTaskTriggerHistory);

    /// <summary>
    /// The storage provider name for PubSubStore.
    /// </summary>
    public const string PubSubStore = nameof(PubSubStore);
}
