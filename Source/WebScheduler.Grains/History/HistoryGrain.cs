namespace WebScheduler.Grains.History;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Abstractions.Services;

/// <summary>
/// Base class for recording history
/// </summary>
/// <typeparam name="TModel"></typeparam>
/// <typeparam name="TOperationType"></typeparam>
[PreferLocalPlacement]
public abstract class HistoryGrain<TModel, TOperationType> : Grain, IHistoryGrain<TModel, TOperationType>
    where TModel : class, IHistoryRecordKeyPrefix, new()
    where TOperationType : Enum
{
    private readonly IExceptionObserver? exceptionObserver;
    private readonly ILogger logger;
    private readonly IPersistentState<HistoryState<TModel, TOperationType>> historyRecordState;
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="exceptionObserver"></param>
    /// <param name="logger"></param>
    /// <param name="state"></param>
    protected HistoryGrain(IExceptionObserver exceptionObserver, ILogger logger, IPersistentState<HistoryState<TModel, TOperationType>> state)
    {
        this.exceptionObserver = exceptionObserver;
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

            await this.historyRecordState.WriteStateAsync();

            // Deactivate the grain 2 minutes from now.
            // This is best effort.
            _ = this.RegisterTimer(_ =>
             {
                 this.DeactivateOnIdle();
                 return Task.CompletedTask;
             }, null, OneMinute, OneMinute);

            return true;
        }
        catch (Exception ex)
        {
            // Reset the state in event of failure
            this.historyRecordState.State = default!;
            this.logger.ErrorRecordingHistory(ex, this.GetPrimaryKeyString());

            await this.exceptionObserver!.OnException(ex);

            return false;
        }
    }
}
