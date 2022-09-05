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
using System;
using WebScheduler.Grains.Diagnostics.Metrics;

/// <summary>
/// A scheduled task grain
/// </summary>
public class ScheduledTaskGrain : Grain, IScheduledTaskGrain, IRemindable, ITenantScopedGrain<IScheduledTaskGrain>, IIncomingGrainCallFilter
{
    private readonly IExceptionObserver exceptionObserver;
    private readonly ILogger<ScheduledTaskGrain> logger;
    private readonly IPersistentState<ScheduledTaskState> taskState;
    private readonly IClockService clockService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IClusterClient clusterClient;
    private const string ScheduledTaskReminderName = "ScheduledTaskExecutor";
    private CronExpression? expression;
    private readonly Stopwatch stopwatch = new();
    private IGrainReminder? scheduledTaskReminder;
    private string scheduledTaskId;
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TenSeconds = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan FifteenSeconds = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="logger">logger</param>
    /// <param name="exceptionObserver"></param>
    /// <param name="clockService">clock</param>
    /// <param name="httpClientFactory">httpClientFactory</param>
    /// <param name="clusterClient">clusterClient</param>
    /// <param name="task">state</param>
    public ScheduledTaskGrain(
        ILogger<ScheduledTaskGrain> logger,
        IExceptionObserver exceptionObserver,
        IClockService clockService, IHttpClientFactory httpClientFactory, IClusterClient clusterClient,
        [PersistentState(StateName.ScheduledTaskState, GrainStorageProviderName.ScheduledTaskState)]
        IPersistentState<ScheduledTaskState> task)
    {
        this.exceptionObserver = exceptionObserver;
        this.logger = logger;
        this.taskState = task;
        this.clockService = clockService;
        this.httpClientFactory = httpClientFactory;
        this.clusterClient = clusterClient;
    }

    private async ValueTask<bool> TryToInitializeReminder()
    {
        if (this.scheduledTaskReminder is not null)
        {
            return true;
        }

        try
        {
            this.scheduledTaskReminder = await this.GetReminder(ScheduledTaskReminderName);
            ScheduledTaskInstruments.ScheduledTaskGetReminderSucceededCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId),
            new KeyValuePair<string, object?>("name", ScheduledTaskReminderName),
        }));
            return true;
        }
        catch (Exception ex)
        {
            ScheduledTaskInstruments.ScheduledTaskGetReminderFailedCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId),
            new KeyValuePair<string, object?>("name", ScheduledTaskReminderName),
            }));

            this.logger.FailedToGetReminder(ex,this.scheduledTaskId);
            await this.exceptionObserver.ObserveException(ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async ValueTask<ScheduledTaskMetadata> CreateAsync(ScheduledTaskMetadata scheduledTaskMetadata)
    {
        this.EnsureInitialTaskState();

        var isReused = this.IsTaskDeleted();
        this.InitializeTask(scheduledTaskMetadata);

        this.SetNextRunAt(this.taskState.State.Task.CreatedAt);

        this.PrepareState(ScheduledTaskOperationType.Create, this.taskState.State.Task.CreatedAt);

        var (_, result) = await this.WriteState();
        if (!result)
        {
            // Reset in memory state by deactivating the grain.
            // We can't just clear the state because this could be a case of the caller re-using a scheduledTaskId
            this.DeactivateOnIdle();
            throw new ErrorCreatingScheduledTaskException();
        }

        if (!await this.EnsureReminder())
        {
            // restore in-memory state to before the changes
            this.DeactivateOnIdle();
            throw new ErrorCreatingScheduledTaskException();
        }

        ScheduledTaskInstruments.ScheduledTaskCreatedCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("recreated", isReused),
            new KeyValuePair<string, object?>("enabled", this.taskState.State.Task.IsEnabled),
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId)
        }));
        return this.taskState.State.Task;
    }

    private async Task<bool> EnsureReminder()
    {
        if(!await this.TryToInitializeReminder())
        {
            return false;
        }

        if (!this.HasEmptyHistoryBuffers() || this.IsTaskEnabled())
        {
            // We still have work to do in the future, so leave reminder ticking.
            if (this.scheduledTaskReminder is not null)
            {
                return true;
            }

            try
            {
                this.scheduledTaskReminder = await this.RegisterOrUpdateReminder(ScheduledTaskReminderName, OneMinute, OneMinute);

                ScheduledTaskInstruments.ScheduledTaskRegisterReminderSucceededCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId),
            new KeyValuePair<string, object?>("name", ScheduledTaskReminderName),
                }));
                return true;
            }
            catch (Exception ex)
            {
                ScheduledTaskInstruments.ScheduledTaskRegisterReminderFailedCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId),
            new KeyValuePair<string, object?>("name", ScheduledTaskReminderName),
                }));

                this.logger.ErrorRegisteringReminder(ex);
                await this.exceptionObserver.OnException(ex);

                return false;
            }
        }

        // If the reminder doesn't exist, nothing to do here.
        if (this.scheduledTaskReminder is null)
        {
            return true;
        }

        // unregister the reminder because it exists.
        try
        {
            await this.UnregisterReminder(this.scheduledTaskReminder);
            this.scheduledTaskReminder = null;

            ScheduledTaskInstruments.ScheduledTaskRegisterUnRegisterReminderSucceededCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId),
            new KeyValuePair<string, object?>("name", ScheduledTaskReminderName),
                }));
            return true;
        }
        catch (Exception ex)
        {
            ScheduledTaskInstruments.ScheduledTaskRegisterUnRegistereminderFailedCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId),
            new KeyValuePair<string, object?>("name", ScheduledTaskReminderName),
                }));
            this.logger.ErrorUnRegisteringReminder(ex);
            await this.exceptionObserver.OnException(ex);

            // We return success if we can't unregister the reminder, we will unregister during the next reminder tick.
            return true;
        }
    }

    private void EnsureInitialTaskState()
    {
        if (this.taskState.State.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("TenantId is empty.");
        }

        if (this.TaskExists())
        {
            this.logger.ScheduledTaskAlreadyExists(this.scheduledTaskId);
            throw new ScheduledTaskAlreadyExistsException(this.scheduledTaskId);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<ScheduledTaskMetadata> UpdateAsync(ScheduledTaskMetadata scheduledTaskMetadata)
    {
        this.EnsureTaskExists();

        var oldTaskState = this.CloneTaskState<ErrorUpdatingScheduledTaskException>();

        this.taskState.State.Task.Description = scheduledTaskMetadata.Description;
        this.taskState.State.Task.HttpTriggerProperties = scheduledTaskMetadata.HttpTriggerProperties;
        this.taskState.State.Task.Name = scheduledTaskMetadata.Name;
        this.taskState.State.Task.IsEnabled = scheduledTaskMetadata.IsEnabled;
        this.taskState.State.Task.TriggerType = scheduledTaskMetadata.TriggerType;

        // if the cron expression changed, we reset NextRunAt to be based off of the modified date
        if (this.taskState.State.Task.CronExpression != scheduledTaskMetadata.CronExpression)
        {
            this.SetNextRunAt(this.taskState.State.Task.ModifiedAt);
            this.taskState.State.Task.CronExpression = scheduledTaskMetadata.CronExpression;
        }

        this.PrepareState(ScheduledTaskOperationType.Update);

        var (_, result) = await this.WriteState();
        if (!result)
        {
            // restore in-memory state to before the changes
            this.taskState.State.Task = oldTaskState;
            throw new ErrorUpdatingScheduledTaskException();
        }

        if (!await this.EnsureReminder())
        {
            // restore in-memory state to before the changes
            this.taskState.State.Task = oldTaskState;
            throw new ErrorUpdatingScheduledTaskException();
        }
        ScheduledTaskInstruments.ScheduledTaskUpdatedCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("enabled", this.taskState.State.Task.IsEnabled),
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId)
        }));
        return this.taskState.State.Task;
    }

    private ScheduledTaskMetadata CloneTaskState<TException>()
        where TException : Exception, new()
    {
        var oldTaskState = JsonSerializer.Deserialize<ScheduledTaskMetadata>(JsonSerializer.Serialize(this.taskState.State.Task));
        if (oldTaskState is null)
        {
            this.logger.ErrorCloningTaskState();
            throw new TException();
        }

        return oldTaskState;
    }

    private void EnsureTaskExists()
    {
        if (!this.TaskExists())
        {
            this.logger.ScheduledTaskNotFound(this.scheduledTaskId);
            throw new ScheduledTaskNotFoundException(this.scheduledTaskId);
        }
    }

    private bool TaskExists() => this.taskState.RecordExists && this.taskState.State.TenantId != Guid.Empty && !this.IsTaskDeleted();

    private bool IsTaskDeleted() => this.taskState.State.IsDeleted;
    private bool IsTaskEnabled() => this.taskState.State.Task.IsEnabled;
    private bool HasNextRunAt() => this.taskState.State.Task.NextRunAt.HasValue;

    private void InitializeTask(ScheduledTaskMetadata scheduledTaskMetadata)
    {
        // Explictly set this incase this task was previously deleted and the id is being re-used.
        this.taskState.State.IsDeleted = false;
        this.taskState.State.Task = scheduledTaskMetadata;

        this.taskState.State.Task.CreatedAt = this.clockService.UtcNow;
    }

    private async Task<(Exception? exception, bool wasSuccessful)> WriteState()
    {
        try
        {
            await this.taskState.WriteStateAsync();
            return (exception: null, wasSuccessful: true);
        }
        catch (Exception ex)
        {
            this.logger.ErrorWritingState(ex, this.scheduledTaskId);
            await this.exceptionObserver.OnException(ex);

            return (exception: null, wasSuccessful: true);
        }
    }
    private void PrepareState(ScheduledTaskOperationType operationType) => this.PrepareState(operationType, this.clockService.UtcNow);

    /// <summary>
    /// Enqueues a <see cref="HistoryState{ScheduledTaskMetadata, ScheduledTaskOperationType}"/> to the buffer.
    /// </summary>
    /// <param name="operationType"></param>
    /// <param name="modifiedAt"></param>
    private void PrepareState(ScheduledTaskOperationType operationType, DateTime modifiedAt)
    {
        this.taskState.State.Task.ModifiedAt = modifiedAt;

        // Clone the current state.
        var currentTaskState = JsonSerializer.Deserialize<ScheduledTaskMetadata>(JsonSerializer.Serialize(this.taskState.State.Task));

        ArgumentNullException.ThrowIfNull(currentTaskState);

        this.taskState.State.HistoryBuffer.Add(new HistoryState<ScheduledTaskMetadata, ScheduledTaskOperationType>()
        {
            State = currentTaskState,
            RecordedAt = this.taskState.State.Task.ModifiedAt,
            Operation = operationType
        });

        // Because the history of the task is stored in a queue outside of the task, we clear the state of the task after we log the history information.
        if (operationType == ScheduledTaskOperationType.Delete)
        {
            this.taskState.State.Task = new();
        }
    }

    private void SetNextRunAt(DateTime fromWhen)
    {
        // We should always have a valid CronExpression.
        this.expression = CronExpression.Parse(this.taskState.State.Task.CronExpression, CronFormat.IncludeSeconds);

        if (!this.IsTaskEnabled())
        {
            this.taskState.State.Task.NextRunAt = null;
            return;
        }

        this.taskState.State.Task.NextRunAt = this.expression.GetNextOccurrence(fromWhen, true);
    }

    /// <inheritdoc/>
    [ReadOnly]
    public ValueTask<ScheduledTaskMetadata> GetAsync()
    {
        this.EnsureTaskExists();

        ScheduledTaskInstruments.ScheduledTaskReadCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId)
        }));

        return new ValueTask<ScheduledTaskMetadata>(this.taskState.State.Task);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync()
    {
        this.EnsureTaskExists();
        var oldTaskState = this.CloneTaskState<ErrorDeletingScheduledTaskException>();

        this.taskState.State.IsDeleted = true;

        this.taskState.State.Task.DeletedAt = this.clockService.UtcNow;

        this.PrepareState(ScheduledTaskOperationType.Delete, this.taskState.State.Task.DeletedAt.Value);

        var (_, result) = await this.WriteState();
        if (!result)
        {
            // restore in-memory state to before the changes
            this.taskState.State.Task = oldTaskState;
            throw new ErrorDeletingScheduledTaskException();
        }

        if (!await this.EnsureReminder())
        {
            // restore in-memory state to before the changes
            this.taskState.State.Task = oldTaskState;
            throw new ErrorDeletingScheduledTaskException();
        }

        ScheduledTaskInstruments.ScheduledTaskDeletedCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId)
        }));
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        this.scheduledTaskId = this.GetPrimaryKeyString();
        // Do nothing if the history buffers are empty and the task is disabled
        if (this.HasEmptyHistoryBuffers() && !this.IsTaskEnabled())
        {
            return;
        }

        await this.TryToInitializeReminder();

        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        await this.TryToInitializeReminder();

        var shouldWrite = await this.ProcessScheduledTaskReminderAsync(status);

        // Let's process the history queue after the timer tick and try to clear any backlogs.
        shouldWrite |= await this.ProcessHistoryQueueAsync<IScheduledTaskHistoryGrain, ScheduledTaskMetadata, ScheduledTaskOperationType>(this.taskState.State.HistoryBuffer, 10);
        shouldWrite |= await this.ProcessHistoryQueueAsync<IScheduledTaskTriggerHistoryGrain, ScheduledTaskTriggerHistory, TaskTriggerType>(this.taskState.State.TriggerHistoryBuffer, 10);
       
        if (shouldWrite)
        {
           await this.WriteState();
        }
        _ = await this.EnsureReminder();
    }
    /// <summary>
    /// Determines if the task should run based on the NextRunAt value.
    /// If NextRunAt is null, we want to not run the task, so we subtract a second to fail the evaluation check.
    /// </summary>
    /// <param name="when"></param>
    private bool ShouldTaskRun(DateTime when) => when >= (this.taskState.State.Task.NextRunAt ?? when.Subtract(TimeSpan.FromSeconds(1)));

    private async Task<bool> ProcessScheduledTaskReminderAsync(TickStatus status)
    {
        var now = this.clockService.UtcNow;
        // We don't have a run scheduled, task is deleted, or it is disabled, do nothing.
        // We may be here for just the history buffer flushing.
        if (!this.HasNextRunAt() || this.IsTaskDeleted() || !this.IsTaskEnabled())
        {
            return false;
        }

        // NextRunAt is in the future, do nothing.
        if (!this.ShouldTaskRun(status.CurrentTickTime))
        {
            return false;
        }

        this.stopwatch.Start();
        var historyRecord = await this.ProcessTaskAsync();
        this.stopwatch.Stop();

        historyRecord.State.CurrentTickTime = status.CurrentTickTime;
        historyRecord.State.Period = status.Period;
        historyRecord.State.FirstTickTime = status.FirstTickTime;
        historyRecord.State.Duration = this.stopwatch.Elapsed;
        this.stopwatch.Reset();

        this.taskState.State.TriggerHistoryBuffer.Add(historyRecord);

        this.taskState.State.Task.LastRunAt = now;

        this.SetNextRunAt(now);

        this.taskState.State.Task.ModifiedAt = now;

        // We won't try to write state here, but at the end of the tick
        return true;
    }

    private bool HasEmptyHistoryBuffers() => this.taskState.State.HistoryBuffer.Count == 0 && this.taskState.State.TriggerHistoryBuffer.Count == 0;

    private async ValueTask<bool> ProcessHistoryQueueAsync<TIRecorderGrainInterface, TStateType, TOperationType>(List<HistoryState<TStateType, TOperationType>> buffer, int batchSize)
        where TIRecorderGrainInterface : IHistoryGrain<TStateType, TOperationType>
        where TStateType : class, IHistoryRecordKeyPrefix, new()
        where TOperationType : Enum
    {
        var shouldWrite = false;
        for (var i = 0; i < batchSize; i++)
        {
            if (buffer.Count == 0)
            {
                return shouldWrite;
            }

            var historyRecord = buffer[0];
            var id = $"{this.scheduledTaskId}-{historyRecord.State.KeyPrefix()}{historyRecord.RecordedAt:u}";

            // 1. Record History. If we fail here it is OK as it is an idempotent operation and we'll get it next time and remove it from the buffer.
            try
            {
                var recorder = this.clusterClient.GetGrain<IHistoryGrain<TStateType, TOperationType>>(id);
                var result = await recorder.RecordAsync(historyRecord);

                // Recorder failed to record.
                if (!result)
                {
                    // try with the next item in the buffer
                    continue;
                }
 
                // 2. Remove from the buffer since the history record was writen
                buffer.RemoveAt(0);
                shouldWrite = true;
            }
            catch (Exception exception)
            {
                // If we error on recording, we'll try again next time around.
                this.logger.ErrorRecordingHistory(exception, id);
                await this.exceptionObserver.OnException(exception);
                continue;
            }
        }
       // 3. History record is already persisted, and we've removed the record from the in-memory history buffer.
       // we will attempt to write it exactly once per reminder tick.
       // if the grain deactivates before we can write state, the persisted history will be a nonop on the next attempt
       return shouldWrite;
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
            client.Timeout = TenSeconds;

            using (var tokenSource = new CancellationTokenSource(FifteenSeconds))
            {
                var response = await client.SendAsync(requestMessage);
                result.State.HttpStatusCode = response.StatusCode;
                result.State.Headers = response.Headers.ToHashSet();
                _ = response.EnsureSuccessStatusCode();

                result.State.HttpContent = await response.Content.ReadAsStringAsync();
            }

            ScheduledTaskInstruments.ScheduledTaskHttpSucceededCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId)
        }));
            result.State.Result = TriggerResult.Success;
        }
        catch (TaskCanceledException ex)
        {
            this.logger.ErrorExecutingHttpTriggerTrimedOut(ex, this.scheduledTaskId);

            ScheduledTaskInstruments.ScheduledTaskHttpTimedOutCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId)
            }));
        }
        catch (Exception ex)
        {
            this.logger.ErrorExecutingHttpTrigger(ex, this.scheduledTaskId);
            ScheduledTaskInstruments.ScheduledTaskHttpTriggerFailedCounts.Add(1, new ReadOnlySpan<KeyValuePair<string, object?>>(new[] {
            new KeyValuePair<string, object?>("tenantId", this.taskState.State.TenantId),
            new KeyValuePair<string, object?>("scheduledTaskId", this.scheduledTaskId)
            }));
            result.State.Error = ex.Message;
            result.State.Result = TriggerResult.Failed;
            await this.exceptionObserver.OnException(ex);
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
                    this.logger.TenantUnauthorized(tenantId, this.scheduledTaskId);
                    throw new UnauthorizedAccessException();
                }

                // Claim the Scheduled Task Id
                if (valid is null && context.ImplementationMethod.Name == nameof(IScheduledTaskGrain.CreateAsync))
                {
                    this.taskState.State.TenantId = tenantIdAsGuid;
                }
            }
        }
        catch (Exception exception)
        {
            this.logger.TenantScopedGrainFilter(exception, tenantId, this.scheduledTaskId);

            await this.exceptionObserver.OnException(exception);

            return;
        }

        // Invoke the grain method and let exceptions flow freely back to the client 😆
        await context.Invoke();
    }
}
