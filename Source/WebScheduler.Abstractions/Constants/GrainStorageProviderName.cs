namespace WebScheduler.Abstractions.Constants;

/// <summary>
/// Constants for Grain Storage provider Names.
/// </summary>
public static class GrainStorageProviderName
{
    /// <summary>
    /// The storage provider name for ScheduledTaskMetadata.
    /// </summary>
    public const string ScheduledTaskMetadata = nameof(ScheduledTaskMetadata);

    /// <summary>
    /// The storage provider name for ScheduledTaskMetadata.
    /// </summary>
    public const string TenantState = nameof(TenantState);

    /// <summary>
    /// The storage provider name for PubSubStore.
    /// </summary>
    public const string PubSubStore = nameof(PubSubStore);
}
