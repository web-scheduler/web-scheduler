namespace WebScheduler.Abstractions.Grains.Scheduler;

using System.Collections.Generic;
using System.Net;

/// <summary>
/// ScheduledTaskTriggerHistory holds the history of a trigger execution.
/// </summary>
public class ScheduledTaskTriggerHistory
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
    /// The headers from the trigger execution response if any are available.
    /// </summary>
    public HashSet<KeyValuePair<string, IEnumerable<string>>>? Headers { get; set; }

    /// <summary>
    /// The StatusCode from the trigger execution response if any are available.
    /// </summary>
    public HttpStatusCode? HttpStatusCode { get; set; }

    /// <summary>
    /// The Reason for the StatusCode from the trigger execution response if any are available.
    /// </summary>
    public string? HttpReasonPhrase { get; set; }
}
