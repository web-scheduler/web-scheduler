namespace WebScheduler.Abstractions.Grains.History;

using Orleans;
using WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// Used to record history of grain states.
/// </summary>
/// <typeparam name="TGrainState">The state to store.</typeparam>
/// <typeparam name="TOperationTypes">The operation that caused the change.</typeparam>
public interface IHistoryGrain<TGrainState, TOperationTypes> : IGrainWithStringKey
    where TGrainState : class
{
    /// <summary>
    /// Records the <typeparamref name="TGrainState"/> as a <see cref="HistoryState{TStateType, TOperationType}"/>.
    /// </summary>
    /// <param name="history">The history to record.</param>
    /// <returns>If the recording was sucessful true is returned, otherwise false.</returns>
    ValueTask<bool> RecordAsync(HistoryState<ScheduledTaskMetadata, ScheduledTaskOperationType> history);
}
