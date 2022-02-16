namespace WebScheduler.Grains.Scheduler;

using Cronos;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using WebScheduler.Abstractions.Constants;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Abstractions.Services;

public class ScheduledTaskGrain : Grain, IScheduledTaskGrain, IRemindable
{
    private readonly ILogger<ScheduledTaskGrain> logger;
    private readonly IPersistentState<ScheduledTaskMetadata> scheduledTaskMetadata;
    private readonly IClockService clockService;

    private const string ReminderName = "ScheduledTaskExecutor";

    public ScheduledTaskGrain(ILogger<ScheduledTaskGrain> logger, [PersistentState(StateName.ScheduledTaskMetadata, GrainStorageProviderName.ScheduledTaskMetadata)] IPersistentState<ScheduledTaskMetadata> scheduledTaskDefinition, IClockService clockService)
    {
        this.logger = logger;
        this.scheduledTaskMetadata = scheduledTaskDefinition;
        this.clockService = clockService;
    }

    /// <inheritdoc/>
    public async ValueTask<ScheduledTaskMetadata> CreateAsync(ScheduledTaskMetadata scheduledTaskMetadata)
    {
        if (this.scheduledTaskMetadata.RecordExists)
        {
            this.logger.ScheduledTaskAlreadyExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskAlreadyExistsException(this.GetPrimaryKey());
        }

        this.scheduledTaskMetadata.State = scheduledTaskMetadata;
        await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(true);

        await this.SetupReminderAsync().ConfigureAwait(true);
        return this.scheduledTaskMetadata.State;
    }

    /// <inheritdoc/>
    public ValueTask<ScheduledTaskMetadata> GetAsync()
    {
        if (!this.scheduledTaskMetadata.RecordExists)
        {
            this.logger.ScheduledTaskDoesNotExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskNotFoundException(this.GetPrimaryKey());
        }

        return new ValueTask<ScheduledTaskMetadata>(this.scheduledTaskMetadata.State);
    }

    /// <inheritdoc/>
    public async ValueTask<ScheduledTaskMetadata> DeleteAsync()
    {
        if (!this.scheduledTaskMetadata.RecordExists)
        {
            this.logger.ScheduledTaskDoesNotExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskNotFoundException(this.GetPrimaryKey());
        }

        this.scheduledTaskMetadata.State.IsDeleted = true;
        this.scheduledTaskMetadata.State.DeletedAt = this.clockService.UtcNow;
        await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(false);

        return this.scheduledTaskMetadata.State;
    }
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Reminders are timers that are persisted to storage, so they are resilient if the node goes down. They
        // should not be used for high-frequency timers their period should be measured in minutes, hours or days.
        await this.SetupReminderAsync().ConfigureAwait(true);
        await base.OnActivateAsync(cancellationToken).ConfigureAwait(true);
    }

    private async Task SetupReminderAsync()
    {
        if (!this.scheduledTaskMetadata.RecordExists)
        { // nothing to do
            return;
        }
        CronExpression expression;
        try
        {
            expression = CronExpression.Parse(this.scheduledTaskMetadata.State.CronExpression, CronFormat.IncludeSeconds);
        }
        catch (CronFormatException ex)
        {
            this.logger.InvalidCronExpression(ex, this.GetPrimaryKeyString(), this.scheduledTaskMetadata.State.CronExpression);
            return;
        }

        var now = this.scheduledTaskMetadata.State.LastRunAt ?? DateTime.UtcNow;

        var nextRun = expression.GetNextOccurrence(now);
        if (nextRun == null)
        {
            await this.UnregisterReminder(await this.GetReminder(ReminderName).ConfigureAwait(true)).ConfigureAwait(true);
            return;
        }
        var dueTime = nextRun - now;
        this.scheduledTaskMetadata.State.NextRunAt = nextRun;

        if (dueTime == null)
        {
            // nothing to do;
            return;
        }
        var secondRun = expression.GetNextOccurrence(nextRun.Value) ?? nextRun.Value.AddMinutes(1);

        var period = (secondRun - nextRun) ?? TimeSpan.FromMinutes(1);

        _ = await this.RegisterOrUpdateReminder(ReminderName, dueTime.Value, period).ConfigureAwait(true);

        await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(true);
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (string.Equals(ReminderName, reminderName, StringComparison.Ordinal))
        {
            this.scheduledTaskMetadata.State.LastRunAt = status.CurrentTickTime;
            await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(true);

            await this.SetupReminderAsync().ConfigureAwait(true);
        }
    }
}
