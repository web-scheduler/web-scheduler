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
using WebScheduler.Grains.Constants;
using System.Diagnostics;

/// <summary>
/// A scheduled task grain
/// </summary>
public class ScheduledTaskGrain : Grain, IScheduledTaskGrain, IRemindable, ITenantScopedGrain<IScheduledTaskGrain>, IIncomingGrainCallFilter
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

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="logger">logger</param>
    /// <param name="clockService">clock</param>
    /// <param name="httpClientFactory">httpClientFactory</param>
    /// <param name="clusterClient">clusterClient</param>
    /// <param name="task">state</param>
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
        if (this.taskState.State.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("TenantId is empty.");
        }

        if (this.taskState.Exists() && !this.taskState.State.IsDeleted)
        {
            this.logger.ScheduledTaskAlreadyExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskAlreadyExistsException(this.GetPrimaryKeyString());
        }

        // Explictly set this incase this task was previously deleted and the id is being re-used.
        this.taskState.State.IsDeleted = false;
        this.taskState.State.Task = scheduledTaskMetadata;

        if (this.taskState.State.Task.CreatedAt == DateTime.MinValue)
        {
            this.taskState.State.Task.CreatedAt = this.clockService.UtcNow;
        }

        this.taskState.State.Task.ModifiedAt = this.taskState.State.Task.CreatedAt;

        this.BuildExpressionAndSetNextRunAt();

        await this.SetupReminderAsync();

        await this.WriteStateAsync(ScheduledTaskOperationType.Create);
        return this.taskState.State.Task;
    }

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

        await this.taskState.WriteStateAsync();

        this.historyReminder = await this.RegisterOrUpdateReminder(HistoryReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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
            await this.DisableReminderAsync(writeState: false);
        }
        else
        {
            await this.SetupReminderAsync();
        }

        await this.WriteStateAsync(ScheduledTaskOperationType.Update);
        return this.taskState.State.Task;
    }
    private bool ShouldDisableReminder() => this.taskState.State.IsDeleted || (this.taskState.Exists() && !this.taskState.State.Task.IsEnabled);
    private void BuildExpressionAndSetNextRunAt(bool resetNextRunAt = false)
    {
        // We should always have a valid CronExpression, which is why we always try to evaluate it on start.
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
        await this.DisableReminderAsync(writeState: false);

        this.taskState.State.Task.DeletedAt = this.clockService.UtcNow;
        this.taskState.State.Task.ModifiedAt = this.taskState.State.Task.DeletedAt.Value;

        await this.WriteStateAsync(ScheduledTaskOperationType.Delete);
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        await this.EnsureHistoryQueueProcessorReminderAsync();

        // No task so nothing to do
        if (!this.taskState.Exists())
        {
            return;
        }

        if (this.taskState.Exists() && !this.taskState.State.IsDeleted)
        {
            this.BuildExpressionAndSetNextRunAt();
        }

        await this.SetupReminderAsync();

        await base.OnActivateAsync();
    }

    private async ValueTask EnsureHistoryQueueProcessorReminderAsync()
    {
        if (this.AreHistoryBuffersEmpty())
        {
            return;
        }

        this.historyReminder = await this.GetReminder(HistoryReminderName) ??
            await this.RegisterOrUpdateReminder(HistoryReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private async ValueTask SetupReminderAsync()
    {
        if (!this.taskState.Exists())
        { // nothing to do
            return;
        }

        if (this.ShouldDisableReminder())
        {
            await this.DisableReminderAsync();
        }

        var nextRun = this.taskState.State.Task.NextRunAt ?? this.taskState.State.Task.CreatedAt;
        if (nextRun == DateTime.MinValue)
        {
            await this.DisableReminderAsync();
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
        _ = await this.RegisterOrUpdateReminder(ScheduledTaskReminderName, dueTime, interval);
        this.taskState.State.Task.ModifiedAt = now;
        await this.taskState.WriteStateAsync();
    }

    private async Task DisableReminderAsync(bool writeState = true)
    {
        var reminder = await this.GetReminder(ScheduledTaskReminderName);
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
            if (!writeState)
            {
                return;
            }
        }
        this.taskState.State.Task.IsEnabled = false;
        this.taskState.State.Task.NextRunAt = null;
        this.taskState.State.Task.ModifiedAt = this.clockService.UtcNow;
        await this.taskState.WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        switch (reminderName)
        {
            case ScheduledTaskReminderName:
                await this.ProcessScheduledTaskReminderNameAsync(status);
                break;
            default:
                this.logger.UnknownReminderName(reminderName);
                break;
        }

        // Let's process the history queue after the timer tick and try to clear any backlogs.
        await this.ProcessHistoryQueuesAsync(batchSizePerQueue: 20);

        // Ensure History buffer reminder is removed if nothing to process.
        if (this.AreHistoryBuffersEmpty())
        {
            this.historyReminder = await this.GetReminder(HistoryReminderName);

            // Disable the reminder if there is no more work to do. It'll get re-registered when new records are available.
            if (this.historyReminder is not null)
            {
                await this.UnregisterReminder(this.historyReminder);
            }
            return;
        }

        // If HistoryBuffer isn't empty, let's ensure our reminder.
        this.historyReminder = await this.RegisterOrUpdateReminder(HistoryReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private async Task ProcessScheduledTaskReminderNameAsync(TickStatus status)
    {
        // We don't have a next run, so disable
        if (this.taskState.State.Task.NextRunAt is null)
        {
            await this.DisableReminderAsync();
            return;
        }
        var now = this.clockService.UtcNow;
        // If our interval exceeds max reminder dueTime, then we setup the reminder for the next interval
        if (this.taskState.State.Task.NextRunAt > now)
        {
            await this.SetupReminderAsync();
            return;
        }

        var sw = new Stopwatch();
        sw.Start();
        var historyRecord = await this.ProcessTaskAsync();
        sw.Stop();

        historyRecord.State.CurrentTickTime = status.CurrentTickTime;
        historyRecord.State.Period = status.Period;
        historyRecord.State.FirstTickTime = status.FirstTickTime;
        historyRecord.State.Duration = sw.Elapsed;

        this.taskState.State.TriggerHistoryBuffer.Add(historyRecord);

        this.taskState.State.Task.LastRunAt = status.CurrentTickTime;
        if (this.expression is null)
        {
            await this.DisableReminderAsync();
            return;
        }

        this.taskState.State.Task.NextRunAt = this.expression.GetNextOccurrence(status.CurrentTickTime);
        this.taskState.State.Task.ModifiedAt = now;
        await this.taskState.WriteStateAsync();

        await this.SetupReminderAsync();
    }

    private bool AreHistoryBuffersEmpty() => this.taskState.State.HistoryBuffer.Count == 0 && this.taskState.State.TriggerHistoryBuffer.Count == 0;
    private async ValueTask ProcessHistoryQueuesAsync(int batchSizePerQueue = 10) => await Task.WhenAll(this.ProcessHistoryQueueAsync<IScheduledTaskHistoryGrain, ScheduledTaskMetadata, ScheduledTaskOperationType>(this.taskState.State.HistoryBuffer, batchSizePerQueue).AsTask(),
           this.ProcessHistoryQueueAsync<IScheduledTaskTriggerHistoryGrain, ScheduledTaskTriggerHistory, TaskTriggerType>(this.taskState.State.TriggerHistoryBuffer, batchSizePerQueue).AsTask());
    private async ValueTask ProcessHistoryQueueAsync<TIRecorderGrainInterface, TStateType, TOperationType>(List<HistoryState<TStateType, TOperationType>> buffer, int batchSize)
        where TIRecorderGrainInterface : IHistoryGrain<TStateType, TOperationType>
        where TStateType : class, IHistoryRecordKeyPrefix, new()
        where TOperationType : Enum
    {
        for (var i = 0; i < batchSize; i++)
        {
            if (buffer.Count == 0)
            {
                return;
            }

            var historyRecord = buffer[0];
            var id = $"{this.GetPrimaryKeyString()}-{historyRecord.State.KeyPrefix()}{historyRecord.RecordedAt:u}";

            // 1. Record History. If we fail here it is OK as it is an idempotent operation and we'll get it next time.
            try
            {
                var recorder = this.clusterClient.GetGrain<IHistoryGrain<TStateType, TOperationType>>(id);
                var result = await recorder.RecordAsync(historyRecord);

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
            buffer.RemoveAt(0);

            // 3. Persist state. If this succeeds, great, we've removed the record from the list.
            // if it fails, that is fine, beacause we reinsert it at the head of the list in the catch block.
            // If our app dies between the WriteStateAsync() and the Insert() that is fine because it still exists in storage
            try
            {
                await this.taskState.WriteStateAsync();
            }
            catch (Exception ex)
            {
                this.logger.ErrorWritingState(ex, this.GetPrimaryKeyString());

                // Add the item back to the list.
                buffer.Insert(0, historyRecord);
            }
        }
    }
    private async Task<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>> ProcessTaskAsync() => this.taskState.State.Task.TriggerType switch
    {
        TaskTriggerType.HttpTrigger => await this.ProcessHttpTriggerAsync(this.taskState.State.Task.HttpTriggerProperties),
        _ => throw new ArgumentOutOfRangeException(nameof(this.taskState.State.Task.TriggerType), this.taskState.State.Task.TriggerType, "Invalid trigger type"),
    };

    private async Task<HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>> ProcessHttpTriggerAsync(HttpTriggerProperties httpTriggerProperties)
    {
        var result = new HistoryState<ScheduledTaskTriggerHistory, TaskTriggerType>()
        {
            RecordedAt = this.clockService.UtcNow,
            Operation = TaskTriggerType.HttpTrigger,
        };

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

            var response = await client.SendAsync(requestMessage);
            result.State.HttpStatusCode = response.StatusCode;
            result.State.Headers = response.Headers.ToHashSet();
            result.State.HttpContent = await response.Content.ReadAsStringAsync();

            _ = response.EnsureSuccessStatusCode();

            result.State.Result = TriggerResult.Success;
        }
        catch (Exception ex)
        {
            this.logger.ErrorExecutingHttpTrigger(ex, this.GetPrimaryKeyString());
            result.State.Error = ex.Message;
            result.State.Result = TriggerResult.Failed;
        }
        finally
        {
            content?.Dispose();
        }

        return result;
    }

    /// <inheritdoc/>
    [ReadOnly]
    public ValueTask<bool?> IsOwnedByAsync(Guid tenantId) => ValueTask.FromResult(this.IsOwnedInternal(tenantId));

    private bool? IsOwnedInternal(Guid tenantId)
    {
        if (this.taskState.State.TenantId == Guid.Empty)
        {
            return null;
        }

        return this.taskState.State.TenantId == tenantId;
    }

    /// <summary>
    /// Invokes this filter.
    /// </summary>
    /// <param name="context">The grain call context.</param>
    /// <returns>A <see cref="Task" /> representing the work performed.</returns>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var tenantId = string.Empty;

        try
        {
            if (context.InterfaceMethod.ReflectedType != typeof(IRemindable))
            {
                tenantId = RequestContext.Get(RequestContextKeys.TenantId) as string ?? throw new ArgumentNullException($"{RequestContextKeys.TenantId} not found in RequestContext");

                var tenantIdAsGuid = Guid.ParseExact(tenantId, "D");

                var valid = this.IsOwnedInternal(tenantIdAsGuid);
                if (valid == false)
                {
                    this.logger.TenantUnauthorized(tenantId, context.Grain.GetPrimaryKeyString());
                    throw new UnauthorizedAccessException();
                }

                // Claim the Scheduled Task Id
                if (valid is null && context.ImplementationMethod.Name == nameof(IScheduledTaskGrain.CreateAsync))
                {
                    this.taskState.State.TenantId = tenantIdAsGuid;
                }
            }
            await context.Invoke();
        }
        catch (Exception exception)
        {
            this.logger.TenantScopedGrainFilter(exception, tenantId, context.Grain.GetPrimaryKeyString());
        }
    }
}
