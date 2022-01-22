namespace WebScheduler.Abstractions.Grains;

using System.ComponentModel.DataAnnotations;
using Orleans;

public interface IScheduledTaskGrain : IGrainWithStringKey
{

 }

public class ScheduledTaskMetadata
{
    [Required]
    [Display(Name = "Task Name", Description = "The name of the task.", ShortName = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Task Description", Description = "The description of what the task does.", ShortName = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name ="Enabled", Description = "Determines if the task is schedulable.")]
    public bool IsEnabled { get; set; }
}
