namespace WebScheduler.Abstractions.Grains.Scheduler;

using System.Collections.Generic;
using System.Net;

/// <summary>
/// ScheduledTaskTriggerHistory holds the history of a trigger execution.
/// </summary>
public class ScheduledTaskTriggerHistory : IHistoryRecordKeyPrefix
{
    /// <summary>
    /// The disposition of the trigger execution.
    /// </summary>
    public TriggerResult Result { get; set; }
    /// <summary>
    /// The duration of the trigger execution.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Any error message resulting from the operation.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The response from the trigger execution if one is available.
    /// </summary>
    public string? HttpContent { get; set; }

    /// <summary>
    /// The StatusCode from the trigger execution response if any are available.
    /// </summary>
    public HttpStatusCode? HttpStatusCode { get; set; }

    /// <summary>
    /// The time on the runtime silo when the silo initiated the delivery of this tick.
    /// </summary>
    public DateTime CurrentTickTime { get; set; }

    /// <summary>
    /// The period of the reminder
    /// </summary>
    public TimeSpan Period { get; set; }

    /// <summary>
    /// The time at which the first tick of this reminder is due, or was triggered
    /// </summary>
    public DateTime FirstTickTime { get; set; }

    /// <summary>
    /// The headers from the trigger execution response if any are available.
    /// </summary>
    public HashSet<KeyValuePair<string, IEnumerable<string>>>? Headers { get; set; }

    /// <summary>
    /// The key prefix.
    /// </summary>
    public string KeyPrefix() => "T";
}
