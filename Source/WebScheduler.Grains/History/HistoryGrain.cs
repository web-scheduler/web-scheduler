namespace WebScheduler.Grains.History;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// Abstract class with grain History implementation for an operation log.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <typeparam name="TOperationType">The operation type.</typeparam>
[PreferLocalPlacement]
public abstract class HistoryGrain<TModel, TOperationType> : Grain, IHistoryGrain<TModel, TOperationType>
    where TModel : class, IHistoryRecordKeyPrefix, new()
    where TOperationType : Enum
{
    private readonly ILogger logger;
    private readonly IPersistentState<HistoryState<TModel, TOperationType>> historyRecordState;

    /// <summary>
    /// The ctor.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="state"></param>
    protected HistoryGrain(ILogger logger, IPersistentState<HistoryState<TModel, TOperationType>> state)
    {
        this.logger = logger;
        this.historyRecordState = state;
    }

    /// <inheritdoc/>
    [ReadOnly]
    public ValueTask<HistoryState<TModel, TOperationType>> GetAsync()
    {
        if (!this.historyRecordState.RecordExists)
        {
            return default;
        }
        return new(this.historyRecordState.State);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> RecordAsync(HistoryState<TModel, TOperationType> history)
    {
        // If we've already recorded the data, it is a retry from a partially failed operation, so discard it.
        if (this.historyRecordState.RecordExists)
        {
            return true;
        }

        try
        {
            this.historyRecordState.State = history;
            await this.historyRecordState.WriteStateAsync().ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            this.logger.ErrorRecordingHistory(ex, this.GetPrimaryKeyString());
            return false;
        }
    }
}
