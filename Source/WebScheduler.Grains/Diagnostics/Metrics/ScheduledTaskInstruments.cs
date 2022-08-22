namespace WebScheduler.Grains.Diagnostics.Metrics;
using System.Diagnostics.Metrics;

internal static class ScheduledTaskInstruments
{
    internal static Counter<int> ScheduledTaskCreatedCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_CREATED);
    internal static Counter<int> ScheduledTaskUpdatedCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_UPDATED);
    internal static Counter<int> ScheduledTaskReadCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_READ);
    internal static Counter<int> ScheduledTaskDeletedCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_DELETED);
    internal static Counter<int> ScheduledTaskEnabledCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_ENABLED);
    internal static Counter<int> ScheduledTaskDisabledCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_DISABLED);

    internal static Counter<int> ScheduledTaskHttpTriggerFailedCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_HTTP_TRIGGER_FAILED);
    internal static Counter<int> ScheduledTaskHttpSucceededCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_HTTP_TRIGGER_SUCCEEDED);
    internal static Counter<int> ScheduledTaskHttpTimedOutCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_HTTP_TRIGGER_TIMED_OUT);

    internal static Counter<int> ScheduledTaskGetReminderSucceededCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_GET_REMINDER_SUCCEEDED);
    internal static Counter<int> ScheduledTaskGetReminderFailedCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_GET_REMINDER_FAILED);

    internal static Counter<int> ScheduledTaskRegisterReminderSucceededCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_REGISTER_REMINDER_SUCCEEDED);
    internal static Counter<int> ScheduledTaskRegisterReminderFailedCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_REGISTER_REMINDER_FAILED);

    internal static Counter<int> ScheduledTaskRegisterUnRegisterReminderSucceededCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_UNREGISTER_REMINDER_SUCCEEDED);
    internal static Counter<int> ScheduledTaskRegisterUnRegistereminderFailedCounts = Instruments.Meter.CreateCounter<int>(InstrumentNames.SCHEDULED_TASK_UNREGISTER_REMINDER_FAILED);
}
