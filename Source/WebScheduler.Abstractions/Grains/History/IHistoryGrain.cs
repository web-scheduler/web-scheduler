namespace WebScheduler.Abstractions.Grains.History;

using Orleans;
using Orleans.Concurrency;

/// <summary>
/// Used to record history of grain states.
/// </summary>
/// <typeparam name="TGrainState">The state to store.</typeparam>
/// <typeparam name="TOperationTypes">The operation that caused the change.</typeparam>
public interface IHistoryGrain<TGrainState, TOperationTypes> : IGrainWithStringKey
    where TGrainState : class, new()
    where TOperationTypes : Enum
{
    /// <summary>
    /// Records the <typeparamref name="TGrainState"/> as a <see cref="HistoryState{TStateType, TOperationType}"/>.
    /// </summary>
    /// <param name="history">The history to record.</param>
    /// <returns>If the recording was sucessful true is returned, otherwise false.</returns>
    ValueTask<bool> RecordAsync(HistoryState<TGrainState, TOperationTypes> history);

    /// <summary>
    /// Gets <typeparamref name="TGrainState"/> as a <see cref="HistoryState{TStateType, TOperationType}"/>.
    /// </summary>
    /// <returns>The history record</returns>
    [ReadOnly]
    ValueTask<HistoryState<TGrainState, TOperationTypes>> GetAsync();
}
