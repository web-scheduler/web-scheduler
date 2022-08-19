namespace WebScheduler.Abstractions.Grains.Scheduler;

using Orleans.CodeGeneration;
using WebScheduler.Abstractions.Grains.History;

/// <summary>
/// Scheduled Task History
/// </summary>
[Version(1)]
public interface IScheduledTaskHistoryGrain : IHistoryGrain<ScheduledTaskMetadata, ScheduledTaskOperationType>
{
}
