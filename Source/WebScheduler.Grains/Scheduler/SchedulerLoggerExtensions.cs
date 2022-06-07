using Microsoft.Extensions.Logging;

/// <summary>
/// <see cref="ILogger"/> extension methods. Helps log messages using strongly typing and source generators.
/// </summary>
internal static partial class SchedulerLoggerExtensions
{
    [LoggerMessage(
        EventId = 6000,
        Level = LogLevel.Warning,
        Message = "Scheduled task {Id} already exists.")]
    public static partial void ScheduledTaskAlreadyExists(this ILogger logger, string id);

    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Warning,
        Message = "Scheduled task {Id} doesn't exist.")]
    public static partial void ScheduledTaskDoesNotExists(this ILogger logger, string id);

    [LoggerMessage(
  EventId = 6002,
  Level = LogLevel.Error,
  Message = "Error executing HttpTrigger for {Id}.")]
    public static partial void ErrorExecutingHttpTrigger(this ILogger logger, Exception exception, string id);
}
