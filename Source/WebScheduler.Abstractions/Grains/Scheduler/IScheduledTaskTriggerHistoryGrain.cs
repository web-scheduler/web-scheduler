namespace WebScheduler.Abstractions.Grains.Scheduler;

using Orleans.CodeGeneration;
using WebScheduler.Abstractions.Grains.History;

/// <summary>
/// ScheduledTaskTrigger history
/// </summary>
[Version(1)]
public interface IScheduledTaskTriggerHistoryGrain : IHistoryGrain<ScheduledTaskTriggerHistory, TaskTriggerType>
{
}
