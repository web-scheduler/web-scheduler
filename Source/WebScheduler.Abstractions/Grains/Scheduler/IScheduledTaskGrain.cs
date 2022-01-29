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
    ValueTask<bool> CreateAsync(ScheduledTaskMetadata scheduledTaskMetadata);

    /// <summary>
    /// Returns the scheduled task.
    /// </summary>
    /// <returns>The scheduled task metadata.</returns>
    ValueTask<ScheduledTaskMetadata?> GetAsync();
}
