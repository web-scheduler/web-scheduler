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
using Polly;
using Polly.CircuitBreaker;

/// <summary>
/// A scheduled task grain
/// </summary>
public class ScheduledTaskGrain : Grain, IScheduledTaskGrain, IRemindable, ITenantScopedGrain<IScheduledTaskGrain>, IIncomingGrainCallFilter
{
    private readonly ILogger<ScheduledTaskGrain> logger;
    private readonly ICommonGrainStoragePolicy policies;
    private readonly IPersistentState<ScheduledTaskState> taskState;
    private readonly IClockService clockService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IClusterClient clusterClient;
    private const string ScheduledTaskReminderName = "ScheduledTaskExecutor";
    private CronExpression? expression;
    private readonly Stopwatch stopwatch = new();
    private IDisposable? retryTimeHandle;
    private IGrainReminder? scheduledTaskReminder;
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="logger">logger</param>
    /// <param name="policies"></param>
    /// <param name="clockService">clock</param>
    /// <param name="httpClientFactory">httpClientFactory</param>
    /// <param name="clusterClient">clusterClient</param>
    /// <param name="task">state</param>
    public ScheduledTaskGrain(ILogger<ScheduledTaskGrain> logger,
        ICommonGrainStoragePolicy policies,
        IClockService clockService, IHttpClientFactory httpClientFactory, IClusterClient clusterClient,
        [PersistentState(StateName.ScheduledTaskState, GrainStorageProviderName.ScheduledTaskState)]
        IPersistentState<ScheduledTaskState> task)
    {
        this.logger = logger;
        this.policies = policies;
        this.taskState = task;
        this.clockService = clockService;
        this.httpClientFactory = httpClientFactory;
        this.clusterClient = clusterClient;
    }

    private async ValueTask TryToInitializeReminder()
    {
        if (this.scheduledTaskReminder is not null)
        {
            return;
        }

        this.scheduledTaskReminder = await this.GetReminder(ScheduledTaskReminderName);
    }

    /// <inheritdoc/>
    public async ValueTask<ScheduledTaskMetadata> CreateAsync(ScheduledTaskMetadata scheduledTaskMetadata)
    {
        this.EnsureInitialTaskState();

        this.InitializeTask(scheduledTaskMetadata);

        this.SetNextRunAt(this.taskState.State.Task.CreatedAt);

        this.PrepareState(ScheduledTaskOperationType.Create, this.taskState.State.Task.CreatedAt);

        if (!await this.WriteState())
        {
            // Reset in memory state by deactivating the grain.
            this.DeactivateOnIdle();
            throw new ErrorCreatingScheduledTaskException();
        }

        if (!await this.EnsureReminder())
        {
            // restore in-memory state to before the changes
            this.DeactivateOnIdle();
            throw new ErrorCreatingScheduledTaskException();
        }
        return this.taskState.State.Task;
    }

    private async Task<bool> EnsureReminder()
    {
        await this.TryToInitializeReminder();

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
                return true;
            }
            catch (Exception ex)
            {
                this.logger.ErrorRegisteringReminder(ex);
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
            return true;
        }
        catch (Exception ex)
        {
            this.logger.ErrorUnRegisteringReminder(ex);
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
            this.logger.ScheduledTaskAlreadyExists(this.GetPrimaryKeyString());
            throw new ScheduledTaskAlreadyExistsException(this.GetPrimaryKeyString());
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

        if (!await this.WriteState())
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
            this.logger.ScheduledTaskNotFound(this.GetPrimaryKeyString());
            throw new ScheduledTaskNotFoundException(this.GetPrimaryKeyString());
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

    private async Task<bool> WriteState(bool shouldRetry = false)
    {
        try
        {
            await this.taskState.WriteStateAsync();
            return true;
        }
        catch (Exception ex)
        {
            this.logger.ErrorWritingState(ex, this.GetPrimaryKeyString());
            if (!shouldRetry)
            {
                return false;
            }

            // Keep state in memory to allow reminders to continue to fire in the event of transient failures at the storage level
            // We'll keep populating the history buffer in an attempt to maintain uptime. In the event the silo crashes any unpersisted state will be lost, which isn't ideal, but it's better than not executing the task.
            // Wait 2 seconds and retry every 30 seconds
            this.retryTimeHandle = this.RegisterTimer(this.RetryWriteStateAsync, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30));
            return false;
        }
    }

    /// <summary>
    /// Writes the grain state to avoid reentrancy in timer callbacks.
    /// </summary>
    public async Task InternalWriteState() => await this.taskState.WriteStateAsync();

    private async Task RetryWriteStateAsync(object arg)
    {
        try
        {
            var policyResult = await this.policies.RetryPolicy
                .ExecuteAndCaptureAsync((_) => this.SelfInvokeAfter<IScheduledTaskGrain>(g => g.InternalWriteState()),
                context: new Context()
                        .WithLogger(this.logger)
                        .WithGrainKey(this.GetPrimaryKeyString()));

            switch (policyResult.Outcome)
            {
                case OutcomeType.Successful:
                    this.retryTimeHandle?.Dispose();
                    break;
                case OutcomeType.Failure:
                    // keep retrying, do nothing
                    break;
                default:
                    // Some other outcome so do nothing to retry
                    break;
            }
        }
        catch (BrokenCircuitException brokenCircuitException)
        {
            this.logger.ErrorWritingStateCircuitBreakerOpen(brokenCircuitException, this.GetPrimaryKeyString());
        }
        catch (Exception)
        {
            throw;
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

        if (!await this.WriteState())
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
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
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

        await this.ProcessScheduledTaskReminderNameAsync(status);

        // Let's process the history queue after the timer tick and try to clear any backlogs.
        await this.ProcessHistoryQueuesAsync(batchSizePerQueue: 20);

        _ = await this.EnsureReminder();
    }
    private bool ShouldTaskRun(DateTime when) => when >= this.taskState.State.Task.NextRunAt;
    private async Task ProcessScheduledTaskReminderNameAsync(TickStatus status)
    {
        // We don't have a run scheduled, task is deleted, or it is disabled, do nothing.
        // We may be here for just the history buffer flushing.
        if (!this.HasNextRunAt() || this.IsTaskDeleted() || !this.IsTaskEnabled() || !this.ShouldTaskRun(this.clockService.UtcNow))
        {
            return;
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

        this.taskState.State.Task.LastRunAt = status.CurrentTickTime;

        this.SetNextRunAt(this.taskState.State.Task.ModifiedAt);

        this.taskState.State.Task.ModifiedAt = this.taskState.State.Task.LastRunAt.Value;

        _ = await this.WriteState(true);
    }

    private bool HasEmptyHistoryBuffers() => this.taskState.State.HistoryBuffer.Count == 0 && this.taskState.State.TriggerHistoryBuffer.Count == 0;
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
                _ = await this.WriteState(true);
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
        }
        catch (Exception exception)
        {
            this.logger.TenantScopedGrainFilter(exception, tenantId, context.Grain.GetPrimaryKeyString());
            return;
        }

        // Invoke the grain method and let exceptions flow freely back to the client 😆
        await context.Invoke();
    }
}
