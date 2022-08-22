namespace WebScheduler.Grains.Diagnostics.Metrics;

internal static class InstrumentNames
{
    // Scheduled Tasks
    public const string SCHEDULED_TASK_CREATED = "webscheduler-scheduled-task-created";
    public const string SCHEDULED_TASK_CREATED_ID_REUSED = "webscheduler-scheduled-task-created-id-reused";
    public const string SCHEDULED_TASK_DELETED = "webscheduler-scheduled-task-deleted";
    public const string SCHEDULED_TASK_UPDATED = "webscheduler-scheduled-task-updated";
    public const string SCHEDULED_TASK_READ = "webscheduler-scheduled-task-read";

    public const string SCHEDULED_TASK_ENABLED = "webscheduler-scheduled-task-enabled";
    public const string SCHEDULED_TASK_DISABLED = "webscheduler-scheduled-task-disabled";

    public const string SCHEDULED_TASK_HTTP_TRIGGER_SUCCEEDED = "webscheduler-scheduled-task-succeeded";
    public const string SCHEDULED_TASK_HTTP_TRIGGER_FAILED = "webscheduler-scheduled-task-failed";
    public const string SCHEDULED_TASK_HTTP_TRIGGER_TIMED_OUT = "webscheduler-scheduled-task-timed-out";

    public const string SCHEDULED_TASK_WRITE_STATE_FAILED = "webscheduler-scheduled-task-write-state-failed";
    public const string SCHEDULED_TASK_WRITE_STATE_SUCCEEDED = "webscheduler-scheduled-task-write-state-succeeded";

    public const string SCHEDULED_TASK_REGISTER_REMINDER_FAILED = "webscheduler-scheduled-task-register-reminder-failed";
    public const string SCHEDULED_TASK_REGISTER_REMINDER_SUCCEEDED = "webscheduler-scheduled-task-register-reminder-succeeded";

    public const string SCHEDULED_TASK_UNREGISTER_REMINDER_FAILED = "webscheduler-scheduled-task-unregister-reminder-failed";
    public const string SCHEDULED_TASK_UNREGISTER_REMINDER_SUCCEEDED = "webscheduler-scheduled-task-unregister-reminder-succeeded";

    public const string SCHEDULED_TASK_GET_REMINDER_FAILED = "webscheduler-scheduled-task-get-reminder-failed";
    public const string SCHEDULED_TASK_GET_REMINDER_SUCCEEDED = "webscheduler-scheduled-task-get-reminder-succeeded";
}
