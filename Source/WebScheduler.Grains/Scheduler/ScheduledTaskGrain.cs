namespace WebScheduler.Grains.Scheduler;

using System.Net.Http;
using Cronos;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using WebScheduler.Abstractions.Constants;
using WebScheduler.Abstractions.Grains;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Abstractions.Services;

public class ScheduledTaskGrain : Grain, IScheduledTaskGrain, IRemindable, ITenentScopedGrain<IScheduledTaskGrain>
{
    private readonly ILogger<ScheduledTaskGrain> logger;
    private readonly IPersistentState<ScheduledTaskMetadata> scheduledTaskMetadata;
    private readonly IPersistentState<TenentState> tenantState;
    private readonly IClockService clockService;
    private readonly IHttpClientFactory httpClientFactory;
    private const string ReminderName = "ScheduledTaskExecutor";
    private CronExpression? expression;

    public ScheduledTaskGrain(ILogger<ScheduledTaskGrain> logger,
        IClockService clockService, IHttpClientFactory httpClientFactory,
        [PersistentState(StateName.ScheduledTaskMetadata, GrainStorageProviderName.ScheduledTaskMetadata)]
    IPersistentState<ScheduledTaskMetadata> scheduledTaskDefinition,
        [PersistentState(StateName.TenentState, GrainStorageProviderName.ScheduledTaskMetadata)]
    IPersistentState<TenentState> tenantState)
    {
        this.logger = logger;
        this.scheduledTaskMetadata = scheduledTaskDefinition;
        this.tenantState = tenantState;
        this.clockService = clockService;
        this.httpClientFactory = httpClientFactory;
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

        this.BuildExpressionAndSetNextRunAt();

        await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(true);

        await this.SetupReminderAsync().ConfigureAwait(true);
        return this.scheduledTaskMetadata.State;
    }

    private void BuildExpressionAndSetNextRunAt()
    {
        // We should always have a valid CronExpression.
        this.expression = CronExpression.Parse(this.scheduledTaskMetadata.State.CronExpression, CronFormat.IncludeSeconds);
        if (this.scheduledTaskMetadata.State.NextRunAt is null)
        {
            this.scheduledTaskMetadata.State.NextRunAt = this.expression.GetNextOccurrence(this.scheduledTaskMetadata.State.CreatedAt, true);
        }
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
        this.scheduledTaskMetadata.State.ModifiedAt = this.scheduledTaskMetadata.State.DeletedAt.Value;
        await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(false);

        return this.scheduledTaskMetadata.State;
    }

    public override async Task OnActivateAsync()
    {
        if (this.scheduledTaskMetadata.RecordExists)
        {
            this.BuildExpressionAndSetNextRunAt();
        }
        await this.SetupReminderAsync().ConfigureAwait(true);
        await base.OnActivateAsync().ConfigureAwait(true);
    }

    private async Task SetupReminderAsync()
    {
        if (!this.scheduledTaskMetadata.RecordExists)
        { // nothing to do
            return;
        }

        var nextRun = this.scheduledTaskMetadata.State.NextRunAt ?? this.scheduledTaskMetadata.State.CreatedAt;
        if (nextRun == DateTime.MinValue)
        {
            await this.DisableReminderAsync().ConfigureAwait(true);
            return;
        }
        var now = this.clockService.UtcNow;
        var dueTime = nextRun - now;

        // cover the scenario when a timer was missed. It'll be negative here.
        if (dueTime.TotalMilliseconds < 0)
        {
            return;
        }
        this.scheduledTaskMetadata.State.NextRunAt = nextRun;

        if (dueTime == TimeSpan.Zero)
        {
            // nothing to do, no interval;
            return;
        }

        if (dueTime.TotalMilliseconds > 0xfffffffe) // Max Timer Interval
        {
            dueTime = TimeSpan.FromMilliseconds(0xfffffffe);
        }

        var secondRun = this.expression?.GetNextOccurrence(nextRun);
        var interval = TimeSpan.FromMinutes(1);
        if (secondRun is not null)
        {
            interval = secondRun.Value - nextRun;
            if (interval.TotalMilliseconds > 0xfffffffe) // Max Timer Interval
            {
                interval = TimeSpan.FromMilliseconds(0xfffffffe);
            }
        }
        _ = await this.RegisterOrUpdateReminder(ReminderName, dueTime, interval).ConfigureAwait(true);
        this.scheduledTaskMetadata.State.ModifiedAt = now;
        await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(true);
    }

    private async Task DisableReminderAsync()
    {
        await this.UnregisterReminder(await this.GetReminder(ReminderName).ConfigureAwait(true)).ConfigureAwait(true);
        this.scheduledTaskMetadata.State.IsEnabled = false;
        this.scheduledTaskMetadata.State.NextRunAt = null;
        this.scheduledTaskMetadata.State.ModifiedAt = this.clockService.UtcNow;
        await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(true);
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (string.Equals(ReminderName, reminderName, StringComparison.Ordinal))
        {
            // We don't have a next run, so disable
            if (this.scheduledTaskMetadata.State.NextRunAt is null)
            {
                await this.DisableReminderAsync().ConfigureAwait(true);
                return;
            }
            var now = this.clockService.UtcNow;
            // If our interval exceeds max reminder dueTime, then we setup the reminder for the next interval
            if (this.scheduledTaskMetadata.State.NextRunAt > now)
            {
                await this.SetupReminderAsync().ConfigureAwait(true);
                return;
            }

            // TODO: Log failures to task history
            _ = await this.ProcessTaskAsync().ConfigureAwait(true);

            this.scheduledTaskMetadata.State.LastRunAt = status.CurrentTickTime;
            if (this.expression is null)
            {
                await this.DisableReminderAsync().ConfigureAwait(true);
                return;
            }

            this.scheduledTaskMetadata.State.NextRunAt = this.expression?.GetNextOccurrence(status.CurrentTickTime);
            this.scheduledTaskMetadata.State.ModifiedAt = now;
            await this.scheduledTaskMetadata.WriteStateAsync().ConfigureAwait(true);

            await this.SetupReminderAsync().ConfigureAwait(true);
        }
    }

    private async Task<bool> ProcessTaskAsync() => this.scheduledTaskMetadata.State.TriggerType switch
    {
        TaskTriggerType.HttpTrigger => await this.ProcessHttpTriggerAsync(this.scheduledTaskMetadata.State.HttpTriggerProperties).ConfigureAwait(true),
        _ => false,// do nothing on unknown task type so we don't break.
    };

    private async Task<bool> ProcessHttpTriggerAsync(HttpTriggerProperties httpTriggerProperties)
    {
        var client = this.httpClientFactory.CreateClient();

        try
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod(httpTriggerProperties.HttpMethod), httpTriggerProperties.EndPointUrl);
            // TODO: Implement other verbs and content body

            var response = await client.SendAsync(requestMessage).ConfigureAwait(true);
            _ = response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error executing HttpTrigger: {Message}", ex.Message);
        }

        return false;
    }

    public async ValueTask<bool?> IsOwnedByAsync(Guid tenantId)
    {
        // If we have ownership data but no scheduledTask exists, delete ownership and allow it to be reclaimed
        if (this.tenantState.RecordExists && !this.scheduledTaskMetadata.RecordExists)
        {
            await this.tenantState.ClearStateAsync().ConfigureAwait(true);
            return null;
        }

        if (this.tenantState.RecordExists)
        {
            return this.tenantState.State.TenentId == tenantId;
        }

        return null;
    }
    public async ValueTask SetOwnedByAsync(Guid tenantId)
    {
        this.tenantState.State.TenentId = tenantId;
        await this.tenantState.WriteStateAsync().ConfigureAwait(true);
    }
}
