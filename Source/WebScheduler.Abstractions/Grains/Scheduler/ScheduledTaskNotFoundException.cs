namespace WebScheduler.Abstractions.Grains.Scheduler;
using System.Runtime.Serialization;
using Orleans;

/// <summary>
/// ScheduledTaskNotFoundException occurs when a task does not exist.
/// </summary>
[GenerateSerializer]
public class ScheduledTaskNotFoundException : Exception
{
    /// <summary>
    /// The Id.
    /// </summary>
    [Id(0)]
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
    public ScheduledTaskNotFoundException(Guid id, Exception? innerException) : base($"Scheduled task with id {id} already exists.", innerException) => this.Id = id;

    /// <inheritdoc/>
    protected ScheduledTaskNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    internal ScheduledTaskNotFoundException(string? message) : base(message)
    {
    }

    internal ScheduledTaskNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
