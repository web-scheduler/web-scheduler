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
    public string Id { get; init; } = string.Empty;

    /// <inheritdoc/>
    protected ScheduledTaskAlreadyExistsException()
    {
    }

    /// <summary>
    /// By Id
    /// </summary>
    /// <param name="id">the Id</param>
    public ScheduledTaskAlreadyExistsException(string id) : base($"Scheduled task with id {id} already exists.") => this.Id = id;

    /// <inheritdoc/>
    public ScheduledTaskAlreadyExistsException(string id, Exception? innerException) : base($"Scheduled task with id {id} already exists.", innerException) => this.Id = id;

    /// <inheritdoc/>
    protected ScheduledTaskAlreadyExistsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
