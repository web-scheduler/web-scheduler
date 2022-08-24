namespace WebScheduler.Abstractions.Grains.Scheduler;
using System.Runtime.Serialization;

/// <summary>
/// HistoryRecordNotFound occurs when a history record does not exist.
/// </summary>
[Serializable]
public class HistoryRecordNotFound : Exception
{
    /// <summary>
    /// The Id.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <inheritdoc/>
    protected HistoryRecordNotFound()
    {
    }

    /// <summary>
    /// By Id
    /// </summary>
    /// <param name="id">the Id</param>
    public HistoryRecordNotFound(string id) : base($"History record with id {id} does not exist.") => this.Id = id;

    /// <inheritdoc/>
    public HistoryRecordNotFound(string id, Exception? innerException) : base($"History record with id {id} does not exist.", innerException) => this.Id = id;

    /// <inheritdoc/>
    protected HistoryRecordNotFound(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
