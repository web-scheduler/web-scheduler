using Microsoft.Extensions.Logging;
using WebScheduler.Grains.Scheduler;

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
    public static partial void ScheduledTaskNotFound(this ILogger logger, string id);

    [LoggerMessage(
        EventId = 6202,
        Level = LogLevel.Debug,
        Message = "Error executing HttpTrigger for {Id}.")]
    public static partial void ErrorExecutingHttpTrigger(this ILogger logger, Exception exception, string id);
    [LoggerMessage(
    EventId = 6203,
    Level = LogLevel.Debug,
    Message = "Executing HttpTrigger for {Id} Timed Out.")]
    public static partial void ErrorExecutingHttpTriggerTrimedOut(this ILogger logger, Exception exception, string id);
    [LoggerMessage(
    EventId = 6204,
    Level = LogLevel.Debug,
    Message = "Failed to get reminder for {Id}.")]
    public static partial void FailedToGetReminder(this ILogger logger, Exception exception, string id);

    [LoggerMessage(
    EventId = 6205,
    Level = LogLevel.Debug,
    Message = "Failed to register reminder for {Id}.")]
    public static partial void FailedToRegisterReminder(this ILogger logger, Exception exception, string id);

    [LoggerMessage(
    EventId = 6206,
    Level = LogLevel.Debug,
    Message = "Failed to unregister reminder for {Id}.")]
    public static partial void FailedToUnRegisterReminder(this ILogger logger, Exception exception, string id);

    [LoggerMessage(
    EventId = 6207,
    Level = LogLevel.Error,
    Message = "Error recording history for ScheduledTask {Id}.")]
    public static partial void ErrorRecordingHistory(this ILogger logger, Exception exception, string id);

    [LoggerMessage(
    EventId = 6004,
    Level = LogLevel.Error,
    Message = "Error writing state for ScheduledTask {Id}.")]
    public static partial void ErrorWritingState(this ILogger<ScheduledTaskGrain> logger, Exception exception, string id);

    [LoggerMessage(
    EventId = 6005,
    Level = LogLevel.Error,
    Message = "Unable to create scheduled task reminder.")]
    public static partial void ErrorRegisteringReminder(this ILogger<ScheduledTaskGrain> logger, Exception ex);

    [LoggerMessage(
    EventId = 6006,
    Level = LogLevel.Error,
    Message = "Unable to unregister scheduled task reminder.")]
    public static partial void ErrorUnRegisteringReminder(this ILogger<ScheduledTaskGrain> logger, Exception ex);

    [LoggerMessage(
    EventId = 6007,
    Level = LogLevel.Error,
    Message = "Unable to clone task state.")]
    public static partial void ErrorCloningTaskState(this ILogger<ScheduledTaskGrain> logger);

    [LoggerMessage(
    EventId = 6008,
    Level = LogLevel.Error,
    Message = "Error writing state for '{Id}'; Retry {RetryCount}.")]
    public static partial void ErrorWritingStateRetry(this ILogger logger, Exception exception, string id, int retryCount);

    [LoggerMessage(
    EventId = 6009,
    Level = LogLevel.Error,
    Message = "Error writing state for '{Id}'; Circuit Breaker triggered.")]
    public static partial void ErrorWritingStateCircuitBreakerOpen(this ILogger logger, Exception exception, string id);
}
