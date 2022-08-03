namespace WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// Provides for a key prefix to be used in history record keys.
/// </summary>
public interface IHistoryRecordKeyPrefix
{
    /// <summary>
    /// The key prefix.
    /// </summary>
    abstract string KeyPrefix();
}
