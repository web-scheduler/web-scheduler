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

/// <summary>
/// Model for Scheduled Task metadata
/// </summary>
public class ScheduledTaskMetadata
{
    /// <summary>
    /// The name of the scheduled task.
    /// </summary>
    [Required]
    [Display(Name = "Task Name", Description = "The name of the task.", ShortName = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the scheduled task.
    /// </summary>
    [Required]
    [Display(Name = "Task Description", Description = "The description of what the task does.", ShortName = "Description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Specifies if the task is enabled.
    /// </summary>
    [Display(Name = "Enabled", Description = "Determines if the task is schedulable.", ShortName = "Enabled")]
    public bool IsEnabled { get; set; }
}
