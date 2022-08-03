namespace WebScheduler.Abstractions.Grains.Scheduler;
using System.Threading.Tasks;
using Orleans;
using Orleans.CodeGeneration;

/// <summary>
/// A scheduled task grain
/// </summary>
[Version(1)]
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
    ValueTask DeleteAsync();

    /// <summary>
    /// Returns the scheduled task.
    /// </summary>
    /// <returns>The scheduled task metadata.</returns>
    ValueTask<ScheduledTaskMetadata> GetAsync();

    /// <summary>
    /// Updates an existing scheduled task
    /// </summary>
    /// <param name="scheduledTaskMetadata">The scheduled task data to update with.</param>
    /// <returns>The updated scheduled task.</returns>
    ValueTask<ScheduledTaskMetadata> UpdateAsync(ScheduledTaskMetadata scheduledTaskMetadata);
}
