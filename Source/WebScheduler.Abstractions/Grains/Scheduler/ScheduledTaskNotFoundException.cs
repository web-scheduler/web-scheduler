namespace WebScheduler.Abstractions.Grains.Scheduler;
using System.Runtime.Serialization;

/// <summary>
/// ScheduledTaskNotFoundException occurs when a task does not exist.
/// </summary>
[Serializable]
public class ScheduledTaskNotFoundException : Exception
{
    /// <summary>
    /// The Id.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <inheritdoc/>
    protected ScheduledTaskNotFoundException()
    {
    }

    /// <summary>
    /// By Id
    /// </summary>
    /// <param name="id">the Id</param>
    public ScheduledTaskNotFoundException(string id) : base($"Scheduled task with id {id} does not exist.") => this.Id = id;

    /// <inheritdoc/>
    public ScheduledTaskNotFoundException(string id, Exception? innerException) : base($"Scheduled task with id {id} does not exist.", innerException) => this.Id = id;

    /// <inheritdoc/>
    protected ScheduledTaskNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
