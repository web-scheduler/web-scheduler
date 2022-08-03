namespace WebScheduler.Grains.Scheduler;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.Scheduler;

public static class IPersistentStateScheduledTaskStateExtensions
{
    /// <summary>
    /// Checks if the given state exists.
    /// </summary>
    /// <param name="state"></param>
    /// <returns>true for yes, false for no.</returns>
    public static bool Exists(this IPersistentState<ScheduledTaskState> state) => state.State.TenantId != Guid.Empty && !state.State.IsDeleted;
}
