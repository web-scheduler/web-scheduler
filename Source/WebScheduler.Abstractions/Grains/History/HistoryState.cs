namespace WebScheduler.Abstractions.Grains.History;

/// <summary>
/// The state object for state History.
/// </summary>
/// <typeparam name="TStateType">The state being stored.</typeparam>
/// <typeparam name="TOperationType">The operation type.</typeparam>
public class HistoryState<TStateType, TOperationType>
    where TStateType : class, new()
    where TOperationType : Enum
{
    /// <summary>
    /// The state in the history object.
    /// </summary>
    public TStateType State { get; set; } = new();

    /// <summary>
    /// When the history record was recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// The history record type.
    /// </summary>
    public TOperationType Operation { get; set; } = default!;
}
