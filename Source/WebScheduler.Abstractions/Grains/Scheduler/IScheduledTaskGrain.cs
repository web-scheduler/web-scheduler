namespace WebScheduler.Abstractions.Grains.Scheduler;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Orleans;

/// <summary>
/// A scheduled task grain
/// </summary>
public interface IScheduledTaskGrain : IGrainWithStringKey
{
    /// <summary>
    /// Creates a new scheduled task.
    /// </summary>
    /// <param name="scheduledTaskMetadata"></param>
    /// <returns><value>true</value> if created, <value>false if it already exists.</value></returns>
    ValueTask<ScheduledTaskMetadata> CreateAsync(ScheduledTaskMetadata scheduledTaskMetadata);

    /// <summary>
    /// Deletes a Scheduled Task Instance.
    /// </summary>
    /// <returns>The deleted task metadata.</returns>
    ValueTask<ScheduledTaskMetadata> DeleteAsync();

    /// <summary>
    /// Returns the scheduled task.
    /// </summary>
    /// <returns>The scheduled task metadata.</returns>
    ValueTask<ScheduledTaskMetadata> GetAsync();
}
