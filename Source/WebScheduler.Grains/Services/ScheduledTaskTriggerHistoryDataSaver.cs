namespace WebScheduler.Grains.Services;

using Microsoft.Extensions.Logging;
using Orleans;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Abstractions.Services;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Batch writes <see cref="ScheduledTaskTriggerHistory"/> records prioritizing trigger failures over successes.
/// Thinking is failures are more valuable than sucesssful trigger deliveries.
/// </summary>
public class ScheduledTaskTriggerHistoryGrainDataSaver :
    BatchedPriorityConcurrentQueueWorker<DataItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, DataServicePriority>, HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, DataServicePriority>
{
    private readonly IClusterClient clusterClient;
    private readonly IExceptionObserver exceptionObserver;

    public ScheduledTaskTriggerHistoryGrainDataSaver(ILogger<ScheduledTaskTriggerHistoryGrainDataSaver> logger, IClusterClient clusterClient, IExceptionObserver exceptionObserver) : base(logger)
    {
        this.MaxCapacity = -1; // Unbounded queue
        this.BatchSize = 10; // Number of grain calls to perform concurrently
        this.ConcurrentFlows = 1; // number of concurrent batches that can be processed in parallel
        this.clusterClient = clusterClient;
        this.exceptionObserver = exceptionObserver;
    }

    /// <summary>
    /// Processes the Task trigger history with concurrency limits.
    /// </summary>
    /// <param name="items"></param>
    /// <param name="cancellationToken"></param>
    protected override async Task<bool> ProcessAsync(ReadOnlyMemory<DataItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, DataServicePriority>> items, CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(BuildTaskList(items));
            return true;
        }
        catch (Exception ex)
        {
            await this.exceptionObserver.ObserveException(ex);
            return false;
        }

        List<Task> BuildTaskList(ReadOnlyMemory<DataItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, DataServicePriority>> items)
        {
            var tasks = new List<Task>(this.BatchSize);
            foreach (var item in items.Span)
            {
                tasks.Add(BuildTask(item, cancellationToken));
            }
            return tasks;
        }
        async Task BuildTask(DataItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, DataServicePriority> item, CancellationToken cancellationToken)
        {
            var recorder = this.clusterClient.GetGrain<IHistoryGrain<ScheduledTaskTriggerHistory, TaskTriggerType>>(item.Key);
            try
            {
                if (!await recorder.RecordAsync(item.Value))
                {
                    // if we fail, add it back to the queue
                    await this.PostOneAsync(item, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await this.exceptionObserver.ObserveException(ex);
            }
        }
    }
}
