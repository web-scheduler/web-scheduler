namespace WebScheduler.Grains.Services;

using Microsoft.Extensions.Logging;
using WebScheduler.Abstractions.Grains.History;
using WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// Batch writes <see cref="ScheduledTaskTriggerHistory"/> records prioritizing trigger failures over successes.
/// Thinking is failures are more valuable than sucesssful trigger deliveries.
/// </summary>
public class ScheduledTaskTriggerHistoryDataSaver :
    DataSavingService<DataItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, DataServicePriority>, HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, DataServicePriority>
{
    public ScheduledTaskTriggerHistoryDataSaver(ILogger<ScheduledTaskTriggerHistoryDataSaver> logger) : base(logger)
    {
        this.MaxCapacity = 10_000;
    }

    protected override async Task<bool> ProcessAsync(ReadOnlyMemory<DataItem<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>, DataServicePriority>> items, CancellationToken cancellationToken)
    {
        try
        {
            // bulk copy proper goes here
            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            this.logger.LogTrace($"Failed to push {items.Length} items: {ex.Message}");
            return false;
        }

        this.logger.LogTrace($"Pushed {items.Length} items");
        return true;
    }
}
