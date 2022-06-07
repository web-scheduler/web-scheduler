namespace WebScheduler.Abstractions.Constants;
/// <summary>
/// Grain state names.
/// </summary>
public static class StateName
{
    /// <summary>
    /// The constant for <see cref="Grains.Scheduler.ScheduledTaskMetadata"/> Grain State.
    /// </summary>
    public const string ScheduledTaskMetadata = nameof(ScheduledTaskMetadata);

    /// <summary>
    /// The constant for <see cref="Grains.TenantState"/> Grain State.
    /// </summary>
    public const string TenantState = nameof(TenantState);
}
