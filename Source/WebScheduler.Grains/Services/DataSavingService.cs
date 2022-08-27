namespace WebScheduler.Grains.Services
{
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Collections.Concurrent;
    using System.Buffers;
    using CommunityToolkit.HighPerformance.Buffers;
    using Microsoft.Extensions.Logging;

    // lower numbers have higher priority in the queue so are dequeued first
    // we use this to discard "success" items first while leaving "failed" items in the queue for later
    public enum DataServicePriority { Low = 0, High= 1 }

    public record DataItem<TModel, TPriority>(string Key, TModel Value, TPriority Status, Func<DateTime> Timestamp) : IDataSavingItem<TModel, TPriority>
        where TModel : class
        where TPriority : Enum;

    public interface IDataSavingItem<TModel, TPriority>
        where TModel : class
        where TPriority : Enum
    {
        string Key { get; }

        Func<DateTime> Timestamp { get; }

        TModel Value { get; }
        TPriority Status { get; }
    }

    public abstract class DataSavingService<TDataItem, TModel, TPriority> : IHostedService
    where TDataItem : IDataSavingItem<TModel, TPriority>
    where TModel : class
    where TPriority : Enum

    {
        /// <summary>
        /// The maximum capacity of the queue.
        /// </summary>
        protected virtual int MaxCapacity { get => this.maxCapacity; set => this.maxCapacity = value; }

        // controls access to the priotity queue
        private readonly SemaphoreSlim queueSemaphore = new(1, 1);

        // controls push readyness
        private readonly SemaphoreSlim pushSemaphore = new(0, 1);

        // this queue orders by discard priority
        // so less important items get dequeued and discarded first while more important items survive until taken for bulk copy
        private readonly PriorityQueue<TDataItem, TDataItem> queue = new(Comparer<TDataItem>.Create(Priority));

        private readonly CancellationTokenSource cancellation = new();
        protected readonly ILogger logger;
        private Task pushTask;
        private int maxCapacity;

        public DataSavingService(ILogger logger)
        {
            this.logger = logger;
        }
        private static int Priority(TDataItem a, TDataItem b)
        {
            // success is lower than failed so it gets discarded first
            var byStatus = a.Status.CompareTo(b.Status);
            if (byStatus != 0)
            {
                return byStatus;
            }

            // older timestamps get discarded first
            var byTimestamp = a.Timestamp().CompareTo(b.Timestamp());
            if (byTimestamp != 0)
            {
                return byTimestamp;
            }

            return 0;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.pushTask = Task.Run(() => this.PushAsync(this.cancellation.Token));

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // cancel the push loop
            this.cancellation.Cancel();

            // observe it
            try
            {
                await this.pushTask;
            }
            catch (OperationCanceledException)
            {
                // noop
            }
            catch (ObjectDisposedException)
            {
                // noop
            }
        }

        // backpressure is minimal here
        // only enough to ensure the priority queue is updated correctly and the items are accepted
        public async Task PostAsync(ReadOnlyMemory<TDataItem> items, CancellationToken cancellationToken = default)
        {
            await this.queueSemaphore.WaitAsync(cancellationToken);
            try
            {
                this.PostCore(items);
            }
            finally
            {
                this.pushSemaphore.Release();
            }
        }

        public async Task PostOneAsync(TDataItem item, CancellationToken cancellationToken = default)
        {
            await this.queueSemaphore.WaitAsync(cancellationToken);
            try
            {
                this.PostCore(item);
            }
            finally
            {
                this.pushSemaphore.Release();
            }
        }

        private void PostCore(ReadOnlyMemory<TDataItem> items)
        {
            var discarded = 0;
            var count = 0;

            try
            {
                // let the priority queue sort them all out
                foreach (var item in items.Span)
                {
                    if (this.queue.Count < this.maxCapacity)
                    {
                        this.queue.Enqueue(item, item);
                    }
                    else
                    {
                        this.queue.EnqueueDequeue(item, item);
                        discarded++;
                    }
                    count++;
                }
            }
            finally
            {
                this.queueSemaphore.Release();
            }

            this.logger.LogTrace($"Received {count} items, discarded {discarded} items over capacity of {this.maxCapacity}");
        }

        private void PostCore(TDataItem item)
        {
            var discarded = 0;
            var count = 0;

            try
            {
                // let the priority queue sort them all out

                if (this.queue.Count < this.maxCapacity)
                {
                    this.queue.Enqueue(item, item);
                }
                else
                {
                    this.queue.EnqueueDequeue(item, item);
                    discarded++;
                }
                count++;
            }
            finally
            {
                this.queueSemaphore.Release();
            }

            this.logger.LogTrace($"Received {count} items, discarded {discarded} items over capacity of {this.maxCapacity}");
        }

        private async Task PushAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // wait for work to be available
                await this.pushSemaphore.WaitAsync(cancellationToken);

                // grab all survivors after priority discarding
                using var buffer = await this.TakeAllAsync(cancellationToken);

                // off they go
                if (!await this.ProcessAsync(buffer.Memory, cancellationToken))
                {
                    // that failed so back in the queue they go
                    await this.PostAsync(buffer.Memory, cancellationToken);

                    // something is wrong so back off a bit
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private async Task<MemoryOwner<TDataItem>> TakeAllAsync(CancellationToken cancellationToken)
        {
            await this.queueSemaphore.WaitAsync(cancellationToken);
            try
            {
                var buffer = MemoryOwner<TDataItem>.Allocate(this.queue.Count);
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer.Span[i] = this.queue.Dequeue();
                }

                this.logger.LogTrace($"Took {buffer.Length} survivors from the queue");

                return buffer;
            }
            finally
            {
                this.queueSemaphore.Release();
            }
        }

        protected abstract Task<bool> ProcessAsync(ReadOnlyMemory<TDataItem> items, CancellationToken cancellationToken);
    }
}
