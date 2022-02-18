namespace WebScheduler.Abstractions.Grains.Scheduler;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The type of task trigger
/// </summary>
public enum TaskTriggerType
{
    /// <summary>
    /// A web hook delivered via HTTP.
    /// </summary>
    [Display(Name = "HTTP Trigger", Description = "Deliver events via HTTP.")]
    HttpTrigger = 0,
}
