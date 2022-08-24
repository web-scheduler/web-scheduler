namespace WebScheduler.Grains.History;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;
using Orleans.Storage;
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
            throw new HistoryRecordNotFound(this.GetPrimaryKeyString());
        }
        return new(this.historyRecordState.State);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> RecordAsync(HistoryState<TModel, TOperationType> history)
    {
        // If we've already recorded the data, it is a retry from a partially failed operation, so discard it.

        var didExceptionHappen = false;
        try
        {
            if (this.historyRecordState.RecordExists)
            {
                return true;
            }
            this.historyRecordState.State = history;

            await this.historyRecordState.WriteStateAsync();

            return true;
        }
        catch (Exception ex)
        {
            didExceptionHappen = true;
            this.logger.ErrorRecordingHistory(ex, this.GetPrimaryKeyString());

            await this.exceptionObserver!.OnException(ex);

            // In the event we get an exception, we will deactivate to clear state and whatever state is in the database will be reloaded.
            // upon the next activation and if the caller attempts to re-write, it'll turn into a non-op.
            this.DeactivateOnIdle();
            return false;
        }
        finally
        {
            // Timers aren't exactly free, there is a CPU cost to them. So we only create them if we sucessfully wrote the state.
            if (!didExceptionHappen)
            {
                _ = this.RegisterTimer(_ =>
                    {
                        this.DeactivateOnIdle();
                        return Task.CompletedTask;
                    }, null, OneMinute, OneMinute);
            }
        }
    }
}
