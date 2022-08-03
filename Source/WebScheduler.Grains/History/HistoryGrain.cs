namespace WebScheduler.Grains.History;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;

[PreferLocalPlacement]
public abstract class HistoryGrain<TModel, TOperationType> : Grain, IHistoryGrain<TModel, TOperationType>
    where TModel : class, IHistoryRecordKeyPrefix, new()
    where TOperationType : Enum
{
    private readonly ILogger logger;
    private readonly IPersistentState<HistoryState<TModel, TOperationType>> historyRecordState;

    protected HistoryGrain(ILogger logger, IPersistentState<HistoryState<TModel, TOperationType>> state)
    {
        this.logger = logger;
        this.historyRecordState = state;
    }

    [ReadOnly]
    public ValueTask<HistoryState<TModel, TOperationType>> GetAsync()
    {
        if (!this.historyRecordState.RecordExists)
        {
            return default;
        }
        return new(this.historyRecordState.State);
    }

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
