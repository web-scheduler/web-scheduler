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
    public Guid Id { get; init; }

    /// <inheritdoc/>
    protected ScheduledTaskNotFoundException()
    {
    }

    /// <summary>
    /// By Id
    /// </summary>
    /// <param name="id">the Id</param>
    public ScheduledTaskNotFoundException(Guid id) : base($"Scheduled task with id {id} already exists.") => this.Id = id;

    /// <inheritdoc/>
    protected ScheduledTaskNotFoundException(Guid id, Exception? innerException) : base($"Scheduled task with id {id} already exists.", innerException) => this.Id = id;

    /// <inheritdoc/>
    public ScheduledTaskNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
