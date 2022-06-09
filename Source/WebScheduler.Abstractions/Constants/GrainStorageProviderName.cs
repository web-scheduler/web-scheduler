namespace WebScheduler.Abstractions.Constants;

/// <summary>
/// Constants for Grain Storage provider Names.
/// </summary>
public static class GrainStorageProviderName
{
    /// <summary>
    /// The storage provider name for ScheduledTaskState.
    /// </summary>
    public const string ScheduledTaskState = nameof(ScheduledTaskState);

    /// <summary>
    /// The storage provider name for ScheduledTaskMetadataHistory.
    /// </summary>
    public const string ScheduledTaskMetadataHistory = nameof(ScheduledTaskMetadataHistory);

    /// <summary>
    /// The storage provider name for PubSubStore.
    /// </summary>
    public const string PubSubStore = nameof(PubSubStore);
}
