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
    BatchedPriorityConcurrentQueueWorker<BatchQueueItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, BatchedPriorityQueuePriority>, HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, BatchedPriorityQueuePriority>
{
    private readonly ILogger<ScheduledTaskTriggerHistoryGrainDataSaver> logger;
    private readonly IClusterClient clusterClient;
    private readonly IExceptionObserver exceptionObserver;

    public ScheduledTaskTriggerHistoryGrainDataSaver(ILogger<ScheduledTaskTriggerHistoryGrainDataSaver> logger, IClusterClient clusterClient, IExceptionObserver exceptionObserver)
    {
        this.MaxCapacity = -1; // Unbounded queue
        this.BatchSize = 10; // Number of grain calls to perform concurrently
        this.ConcurrentFlows = 1; // number of concurrent batches that can be processed in parallel
        this.logger = logger;
        this.clusterClient = clusterClient;
        this.exceptionObserver = exceptionObserver;
    }

    /// <summary>
    /// Processes the Task trigger history with concurrency limits.
    /// </summary>
    /// <param name="items"></param>
    /// <param name="cancellationToken"></param>
    protected override async Task<bool> ProcessAsync(ReadOnlyMemory<BatchQueueItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, BatchedPriorityQueuePriority>> items, CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(BuildTaskList(items));
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing Task trigger history");
            await this.exceptionObserver.ObserveException(ex);
            return false;
        }

        List<Task> BuildTaskList(ReadOnlyMemory<BatchQueueItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, BatchedPriorityQueuePriority>> items)
        {
            var tasks = new List<Task>(this.BatchSize);
            foreach (var item in items.Span)
            {
                tasks.Add(BuildTask(item, cancellationToken));
            }
            return tasks;
        }
        async Task BuildTask(BatchQueueItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, BatchedPriorityQueuePriority> item, CancellationToken cancellationToken)
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
                this.logger.LogError(ex, "Error processing Task trigger history");
                await this.exceptionObserver.ObserveException(ex);
            }
        }
    }
}
