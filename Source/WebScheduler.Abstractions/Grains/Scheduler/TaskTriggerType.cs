namespace WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// The type of task trigger
/// </summary>
public enum TaskTriggerType
{
    /// <summary>
    /// A web hook.
    /// </summary>
    HttpUri = 0,
}
