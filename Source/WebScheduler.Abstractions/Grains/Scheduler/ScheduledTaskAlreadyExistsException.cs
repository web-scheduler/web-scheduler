namespace WebScheduler.Abstractions.Grains.Scheduler;
using System.Runtime.Serialization;
using Orleans;

/// <summary>
/// ScheduledTaskAlreadyExists occurs when a task does already exist.
/// </summary>
[GenerateSerializer]
public class ScheduledTaskAlreadyExistsException : Exception
{
    /// <summary>
    /// The Id.
    /// </summary>
    [Id(0)]
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

    internal ScheduledTaskAlreadyExistsException(string? message) : base(message)
    {
    }

    internal ScheduledTaskAlreadyExistsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
