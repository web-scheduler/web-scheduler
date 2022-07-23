namespace WebScheduler.Grains.Scheduler;

using System.Net.Http;
using Cronos;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System.Text;
using WebScheduler.Abstractions.Constants;
using WebScheduler.Abstractions.Grains;
using WebScheduler.Abstractions.Services;
using WebScheduler.Abstractions.Grains.Scheduler;
using WebScheduler.Abstractions.Grains.History;
using System.Text.Json;
using Orleans.Concurrency;

public class ScheduledTaskGrain : Grain, IScheduledTaskGrain, IRemindable, ITenantScopedGrain<IScheduledTaskGrain>
{
    private readonly ILogger<ScheduledTaskGrain> logger;
    private readonly IPersistentState<ScheduledTaskState> taskState;
    private readonly IClockService clockService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IClusterClient clusterClient;
    private const string ScheduledTaskReminderName = "ScheduledTaskExecutor";
    private const string HistoryReminderName = "HistoryQueueProcessor";
    private CronExpression? expression;
    private IGrainReminder? historyReminder;

    public ScheduledTaskGrain(ILogger<ScheduledTaskGrain> logger,
        IClockService clockService, IHttpClientFactory httpClientFactory, IClusterClient clusterClient,
        [PersistentState(StateName.ScheduledTaskState, GrainStorageProviderName.ScheduledTaskState)]
        IPersistentState<ScheduledTaskState> task)
    {
        this.logger = logger;
        this.taskState = task;
        this.clockService = clockService;
        this.httpClientFactory = httpClientFactory;
        this.clusterClient = clusterClient;
    }

    /// <inheritdoc/>
    public async ValueTask<ScheduledTaskMetadata> CreateAsync(ScheduledTaskMetadata scheduledTaskMetadata)
    {
        if (this.taskState.Exists() && !this.taskState.State.IsDeleted)
        {
            this.logger.ScheduledTaskAlreadyExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskAlreadyExistsException(this.GetPrimaryKeyString());
        }

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        var tenantId = RequestContext.Get(RequestContextKeys.TenantId) as string ?? throw new ArgumentNullException($"{RequestContextKeys.TenantId} not found in RequestContext");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

        this.taskState.State.TenantId = Guid.ParseExact(tenantId, "D");

        // Explictly set this incase this task was previously deleted and the id is being re-used.
        this.taskState.State.IsDeleted = false;
        this.taskState.State.Task = scheduledTaskMetadata;

        if (this.taskState.State.Task.CreatedAt == DateTime.MinValue)
        {
            this.taskState.State.Task.CreatedAt = this.clockService.UtcNow;
        }

        this.taskState.State.Task.ModifiedAt = this.taskState.State.Task.CreatedAt;

        this.BuildExpressionAndSetNextRunAt();

        await this.SetupReminderAsync().ConfigureAwait(true);

        await this.WriteStateAsync(ScheduledTaskOperationType.Create).ConfigureAwait(true);
        return this.taskState.State.Task;
    }

    /// <summary>
    /// Enqueues a <see cref="HistoryState{TStateType}"/> to the buffer and writes the states to storage.
    /// </summary>
    /// <param name="operationType"></param>
    private async ValueTask WriteStateAsync(ScheduledTaskOperationType operationType)
    {
        // Clone the current state.
        var currentState = JsonSerializer.Deserialize<ScheduledTaskMetadata>(JsonSerializer.Serialize(this.taskState.State.Task));

        ArgumentNullException.ThrowIfNull(currentState);

        this.taskState.State.HistoryBuffer.Add(new HistoryState<ScheduledTaskMetadata, ScheduledTaskOperationType>()
        {
            State = currentState,
            RecordedAt = this.taskState.State.Task.ModifiedAt,
            Operation = operationType
        });

        // Because the history of the task is stored in a queue outside of the task, we clear the state of the task after we log the history information.
        if (operationType == ScheduledTaskOperationType.Delete)
        {
            this.taskState.State.Task = new();
        }

        await this.taskState.WriteStateAsync().ConfigureAwait(true);

        this.historyReminder = await this.RegisterOrUpdateReminder(HistoryReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)).ConfigureAwait(true);
    }

    /// <inheritdoc/>
    public async ValueTask<ScheduledTaskMetadata> UpdateAsync(ScheduledTaskMetadata scheduledTaskMetadata)
    {
        if (!this.taskState.Exists())
        {
            this.logger.ScheduledTaskAlreadyExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskNotFoundException(this.GetPrimaryKeyString());
        }

        this.taskState.State.Task.CronExpression = scheduledTaskMetadata.CronExpression;
        this.taskState.State.Task.Description = scheduledTaskMetadata.Description;
        this.taskState.State.Task.HttpTriggerProperties = scheduledTaskMetadata.HttpTriggerProperties;
        this.taskState.State.Task.Name = scheduledTaskMetadata.Name;
        this.taskState.State.Task.IsEnabled = scheduledTaskMetadata.IsEnabled;
        this.taskState.State.Task.TriggerType = scheduledTaskMetadata.TriggerType;
        this.taskState.State.Task.ModifiedAt = this.clockService.UtcNow;

        this.BuildExpressionAndSetNextRunAt(resetNextRunAt: true);

        if (this.ShouldDisableReminder())
        {
            await this.DisableReminderAsync(writeState: false).ConfigureAwait(true);
        }
        else
        {
            await this.SetupReminderAsync().ConfigureAwait(true);
        }

        await this.WriteStateAsync(ScheduledTaskOperationType.Update).ConfigureAwait(true);
        return this.taskState.State.Task;
    }
    private bool ShouldDisableReminder() => this.taskState.State.IsDeleted || (this.taskState.Exists() && !this.taskState.State.Task.IsEnabled);
    private void BuildExpressionAndSetNextRunAt(bool resetNextRunAt = false)
    {
        // We should always have a valid CronExpression.
        this.expression = CronExpression.Parse(this.taskState.State.Task.CronExpression, CronFormat.IncludeSeconds);

        if (!this.taskState.State.Task.IsEnabled)
        {
            this.taskState.State.Task.NextRunAt = null;
            return;
        }

        if (this.taskState.State.Task.NextRunAt is null || resetNextRunAt)
        {
            this.taskState.State.Task.NextRunAt = this.expression.GetNextOccurrence(this.taskState.State.Task.ModifiedAt, true);
        }
    }

    /// <inheritdoc/>
    [ReadOnly]
    public ValueTask<ScheduledTaskMetadata> GetAsync()
    {
        if (!this.taskState.Exists())
        {
            this.logger.ScheduledTaskDoesNotExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskNotFoundException(this.GetPrimaryKeyString());
        }

        return new ValueTask<ScheduledTaskMetadata>(this.taskState.State.Task);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync()
    {
        if (!this.taskState.Exists())
        {
            this.logger.ScheduledTaskDoesNotExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskNotFoundException(this.GetPrimaryKeyString());
        }

        this.taskState.State.IsDeleted = true;
        await this.DisableReminderAsync(writeState: false).ConfigureAwait(true);

        this.taskState.State.Task.DeletedAt = this.clockService.UtcNow;
        this.taskState.State.Task.ModifiedAt = this.taskState.State.Task.DeletedAt.Value;

        await this.WriteStateAsync(ScheduledTaskOperationType.Delete).ConfigureAwait(true);
    }

    public override async Task OnActivateAsync()
    {
        // No task so nothing to do
        if (!this.taskState.Exists())
        {
            return;
        }

        if (this.taskState.Exists() && !this.taskState.State.IsDeleted)
        {
            this.BuildExpressionAndSetNextRunAt();
        }
        await this.SetupReminderAsync().ConfigureAwait(true);

        this.historyReminder = await this.GetReminder(HistoryReminderName).ConfigureAwait(true);
        if (this.historyReminder is null && this.taskState.State.HistoryBuffer.Count > 0)
        {
            this.historyReminder = await this.RegisterOrUpdateReminder(HistoryReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)).ConfigureAwait(true);
        }

        await base.OnActivateAsync().ConfigureAwait(true);
    }

    private async ValueTask SetupReminderAsync()
    {
        if (!this.taskState.Exists())
        { // nothing to do
            return;
        }

        if (this.ShouldDisableReminder())
        {
            await this.DisableReminderAsync().ConfigureAwait(true);
        }

        var nextRun = this.taskState.State.Task.NextRunAt ?? this.taskState.State.Task.CreatedAt;
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
        this.taskState.State.Task.NextRunAt = nextRun;

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
        _ = await this.RegisterOrUpdateReminder(ScheduledTaskReminderName, dueTime, interval).ConfigureAwait(true);
        this.taskState.State.Task.ModifiedAt = now;
        await this.taskState.WriteStateAsync().ConfigureAwait(true);
    }

    private async Task DisableReminderAsync(bool writeState = true)
    {
        var reminder = await this.GetReminder(ScheduledTaskReminderName).ConfigureAwait(true);
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder).ConfigureAwait(true);
            if (!writeState)
            {
                return;
            }
        }
        this.taskState.State.Task.IsEnabled = false;
        this.taskState.State.Task.NextRunAt = null;
        this.taskState.State.Task.ModifiedAt = this.clockService.UtcNow;
        await this.taskState.WriteStateAsync().ConfigureAwait(true);
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        switch (reminderName)
        {
            case ScheduledTaskReminderName:
                // We don't have a next run, so disable
                if (this.taskState.State.Task.NextRunAt is null)
                {
                    await this.DisableReminderAsync().ConfigureAwait(true);
                    break;
                }
                var now = this.clockService.UtcNow;
                // If our interval exceeds max reminder dueTime, then we setup the reminder for the next interval
                if (this.taskState.State.Task.NextRunAt > now)
                {
                    await this.SetupReminderAsync().ConfigureAwait(true);
                    break;
                }

                // TODO: Log failures to task history
                _ = await this.ProcessTaskAsync().ConfigureAwait(true);

                this.taskState.State.Task.LastRunAt = status.CurrentTickTime;
                if (this.expression is null)
                {
                    await this.DisableReminderAsync().ConfigureAwait(true);
                    break;
                }

                this.taskState.State.Task.NextRunAt = this.expression?.GetNextOccurrence(status.CurrentTickTime);
                this.taskState.State.Task.ModifiedAt = now;
                await this.taskState.WriteStateAsync().ConfigureAwait(true);

                await this.SetupReminderAsync().ConfigureAwait(true);
                break;
            case HistoryReminderName:
                // Let's process the history queue
                await this.ProcessHistoryQueueAsync(batchSize: 10).ConfigureAwait(true);
                break;
        }

        // Ensure History buffer can empty out
        if (this.taskState.State.HistoryBuffer.Count == 0)
        {
            this.historyReminder = await this.GetReminder(HistoryReminderName).ConfigureAwait(true);

            // Disable the reminder if there is no more work to do. It'll get re-registered when new records are available.
            await this.UnregisterReminder(this.historyReminder).ConfigureAwait(true);

            return;
        }

        // If HistoryBuffer isn't empty, let's ensure our reminder.
        this.historyReminder = await this.RegisterOrUpdateReminder(HistoryReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)).ConfigureAwait(true);
    }

    private async ValueTask ProcessHistoryQueueAsync(int batchSize)
    {
        for (var i = 0; i < batchSize; i++)
        {
            if (this.taskState.State.HistoryBuffer.Count == 0)
            {
                return;
            }

            var historyRecord = this.taskState.State.HistoryBuffer[0];
            var id = $"{this.GetPrimaryKeyString()}-{historyRecord.RecordedAt:u}";

            // 1. Record History. If we fail here it is OK as it is an idempotent operation and we'll get it next time.
            try
            {
                var recorder = this.clusterClient.GetGrain<IHistoryGrain<ScheduledTaskMetadata, ScheduledTaskOperationType>>(id);
                var result = await recorder.RecordAsync(historyRecord).ConfigureAwait(true);

                // Recorder failed to record.
                if (!result)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                // If we error on recording.
                this.logger.ErrorRecordingHistory(ex, id);
            }

            // 2. Remove from the buffer
            this.taskState.State.HistoryBuffer.RemoveAt(0);

            // 3. Persist state. If this succeeds, great, we've removed the record from the list.
            // if it fails, that is fine, beacause we reinsert it at the head of the list in the catch block.
            // If our app dies between the WriteStateAsync() and the Insert() that is fine because it still exists in storage
            try
            {
                await this.taskState.WriteStateAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                this.logger.ErrorWritingState(ex, this.GetPrimaryKeyString());

                // Add the item back to the list.
                this.taskState.State.HistoryBuffer.Insert(0, historyRecord);
            }
        }
    }
    private async Task<bool> ProcessTaskAsync() => this.taskState.State.Task.TriggerType switch
    {
        TaskTriggerType.HttpTrigger => await this.ProcessHttpTriggerAsync(this.taskState.State.Task.HttpTriggerProperties).ConfigureAwait(true),
        _ => false,// do nothing on unknown task type so we don't break.
    };

    private async Task<bool> ProcessHttpTriggerAsync(HttpTriggerProperties httpTriggerProperties)
    {
        var client = this.httpClientFactory.CreateClient();

        StringContent? content = null;

        try
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod(httpTriggerProperties.HttpMethod), httpTriggerProperties.EndPointUrl);

            // Add headers
            var contentType = string.Empty;
            foreach (var header in httpTriggerProperties.Headers)
            {
                // Content-Type is one of the special headers. Let's pull it out as it needs to be used as the Media Content Type on the request body.
                if (header.Key.Equals(Microsoft.Net.Http.Headers.HeaderNames.ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    contentType = header.Value;
                    continue;
                }

                requestMessage.Headers.Add(header.Key, header.Value);
            }

            // Add body if it exists
            if (httpTriggerProperties.RequestBody is not null &&
                (requestMessage.Method == HttpMethod.Post ||
                requestMessage.Method == HttpMethod.Put ||
                requestMessage.Method == HttpMethod.Patch))
            {
                if (contentType != string.Empty)
                {
                    content = new StringContent(httpTriggerProperties.RequestBody, Encoding.UTF8, contentType);
                }
                else
                {
                    content = new StringContent(httpTriggerProperties.RequestBody, Encoding.UTF8);
                }

                requestMessage.Content = content;
            }

            var response = await client.SendAsync(requestMessage).ConfigureAwait(true);
            _ = response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            this.logger.ErrorExecutingHttpTrigger(ex, this.GetPrimaryKeyString());
        }
        finally
        {
            if (content is not null)
            {
                content.Dispose();
            }
        }

        return false;
    }

    [ReadOnly]
    public ValueTask<bool?> IsOwnedByAsync(Guid tenantId)
    {
        if (this.taskState.State.TenantId == Guid.Empty)
        {
            return new();
        }

        return new(this.taskState.State.TenantId == tenantId);
    }
}
public static class IPersistentStateScheduledTaskStateExtensions
{
    /// <summary>
    /// Checks if the given state exists.
    /// </summary>
    /// <param name="state"></param>
    /// <returns>true for yes, false for no.</returns>
    public static bool Exists(this IPersistentState<ScheduledTaskState> state) => state.State.TenantId != Guid.Empty && !state.State.IsDeleted;
}
