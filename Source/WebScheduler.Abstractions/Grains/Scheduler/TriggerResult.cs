namespace WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// The result of the trigger execution.
/// </summary>
public enum TriggerResult
{
    /// <summary>
    /// Unset value, invalid result.
    /// </summary>
    Unset = 0,
    /// <summary>
    /// Failed execution.
    /// </summary>
    Failed = 1,
    /// <summary>
    /// Successful execution.
    /// </summary>
    Success = 2,
}
