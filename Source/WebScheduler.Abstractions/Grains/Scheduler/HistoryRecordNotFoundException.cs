namespace WebScheduler.Abstractions.Grains.Scheduler;
using System.Runtime.Serialization;

/// <summary>
/// HistoryRecordNotFound occurs when a history record does not exist.
/// </summary>
[Serializable]
public class HistoryRecordNotFoundException : Exception
{
    /// <summary>
    /// The Id.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <inheritdoc/>
    protected HistoryRecordNotFoundException()
    {
    }

    /// <summary>
    /// By Id
    /// </summary>
    /// <param name="id">the Id</param>
    public HistoryRecordNotFoundException(string id) : base($"History record with id {id} does not exist.") => this.Id = id;

    /// <inheritdoc/>
    public HistoryRecordNotFoundException(string id, Exception? innerException) : base($"History record with id {id} does not exist.", innerException) => this.Id = id;

    /// <inheritdoc/>
    protected HistoryRecordNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
