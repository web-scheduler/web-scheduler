namespace WebScheduler.Abstractions.Grains.Scheduler;
using System.Runtime.Serialization;

/// <summary>
/// ScheduledTaskAlreadyExists occurs when a task does already exist.
/// </summary>
[Serializable]
public class ScheduledTaskAlreadyExistsException : Exception
{
    /// <summary>
    /// The Id.
    /// </summary>
    public Guid Id { get; init; }

    /// <inheritdoc/>
    protected ScheduledTaskAlreadyExistsException()
    {
    }

    /// <summary>
    /// By Id
    /// </summary>
    /// <param name="id">the Id</param>
    public ScheduledTaskAlreadyExistsException(Guid id) : base($"Scheduled task with id {id} already exists.") => this.Id = id;

    /// <inheritdoc/>
    public ScheduledTaskAlreadyExistsException(Guid id, Exception? innerException) : base($"Scheduled task with id {id} already exists.", innerException) => this.Id = id;

    /// <inheritdoc/>
    protected ScheduledTaskAlreadyExistsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
