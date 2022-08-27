namespace WebScheduler.Grains.Services
{
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Collections.Concurrent;
    using System.Buffers;
    using CommunityToolkit.HighPerformance.Buffers;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.Threading;
    using System.Runtime.CompilerServices;
    using DotNext.Threading;

    /// <summary>
    /// lower numbers have higher priority in the queue so are dequeued first
    /// we use this to discard "success" items first while leaving "failed" items in the queue for later
    /// </summary>
    public enum DataServicePriority { Low = 0, High = 1 }

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

    /// <summary>
    /// A concurrent priority queue that supports batching, batch processing concurrency limits, bounded and unbounded queue sizes, and a flush interval which overrides the batch size.
    /// </summary>
    /// <typeparam name="TDataItem"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TPriority"></typeparam>
    public abstract class BatchedPriorityConcurrentQueue<TDataItem, TModel, TPriority> : IHostedService
    where TDataItem : IDataSavingItem<TModel, TPriority>
    where TModel : class
    where TPriority : Enum

    {
        /// <summary>
        /// The maximum capacity of the queue. Set to <value>-1</value> for an unbounded queue.
        /// </summary>
        protected virtual int MaxCapacity { get; set; }

        /// <summary>
        /// The number of items before the queue is considered full enough to start processing.
        /// </summary>
        protected virtual int BatchSize { get; set; } = 10;

        /// <summary>
        /// The interval of time to process a batch of items even if <see cref="BatchSize"/> is not reached.
        /// </summary>
        protected virtual TimeSpan BatchFlushInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The number of concurrent writers to allow.
        /// </summary>
        protected virtual int ConcurrentFlows { get; set; } = 1;

        private DateTime lastBatchFlush = DateTime.MinValue;

        /// <summary>
        /// controls access to the priotity queue.
        /// </summary>
        private readonly AsyncSemaphore queueSemaphore = new(1);

        /// <summary>
        /// controls push readyness.
        /// </summary>
        private readonly AsyncTrigger pushEvent = new();

        /// <summary>
        /// this queue orders by discard priority
        /// so less important items get dequeued and discarded first while more important items survive until taken for bulk copy
        /// </summary>
        private readonly PriorityQueue<TDataItem, TDataItem> queue;

        private CancellationTokenSource? cancellation;
        /// <summary>
        /// The logger instance.
        /// </summary>
        protected readonly ILogger logger;
        private Task pushTask = default!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger">the logger</param>
        /// <param name="initialQueueCapacity">The initial capacity of the <see cref="PriorityQueue{TElement, TPriority}"/></param>
        public BatchedPriorityConcurrentQueue(ILogger logger, int initialQueueCapacity = 1_000)
        {
            this.logger = logger;
            this.queue = new(initialQueueCapacity, Comparer<TDataItem>.Create(Priority));
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
        
        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.pushTask = Task.Run(() => this.PushAsync(this.cancellation.Token), this.cancellation.Token);

            return Task.CompletedTask;
        }
        
        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // cancel the push loop
            this.cancellation?.Cancel();

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

        /// <summary>
        /// backpressure is minimal here
        /// only enough to ensure the priority queue is updated correctly and the items are accepted
        /// </summary>
        /// <param name="items"></param>
        /// <param name="cancellationToken"></param>
        public async Task PostAsync(ReadOnlyMemory<TDataItem> items, CancellationToken cancellationToken = default)
        {
            using var _ = await this.queueSemaphore.EnterAsync(cancellationToken);

            this.PostCore(items);
        }

        /// <summary>
        /// Posts a single item to the queue.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task PostOneAsync(TDataItem item, CancellationToken cancellationToken = default)
        {
            using var _ = await this.queueSemaphore.EnterAsync(cancellationToken);

            this.PostCore(item);
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
                    if (this.ShouldEnqueue())
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
                this.SignalPushQueue();

            }

            this.logger.LogTrace($"Received {count} items, discarded {discarded} items over capacity of {this.MaxCapacity}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldEnqueue()
        {
            // check if we've met the batch size or the batch flush interval and signal the push queue to trigger.
            if (this.queue.Count < this.MaxCapacity || this.MaxCapacity < 0)
            {
                return true;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SignalPushQueue()
        {
            // check if we've met the batch size or the batch flush interval and signal the push queue to trigger.
            if (this.queue.Count >= this.BatchSize || (this.lastBatchFlush + this.BatchFlushInterval) < DateTime.UtcNow)
            {
                this.pushEvent.Signal();
            }
        }
        private void PostCore(TDataItem item)
        {
            var discarded = 0;
            var count = 0;

            try
            {

                if (this.ShouldEnqueue())
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
                this.SignalPushQueue();
            }

            this.logger.LogTrace($"Received {count} items, discarded {discarded} items over capacity of {this.MaxCapacity}");
        }

        private async Task PushAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // wait for work to be available
                await this.pushEvent.WaitAsync(cancellationToken);
                // grab all survivors after priority discarding
                using var buffer = await this.TakeAllAsync(cancellationToken);

                // off they go
                try
                {
                    if (!await this.ProcessAsync(buffer.Memory, cancellationToken))
                    {
                        // that failed so back in the queue they go
                        await this.PostAsync(buffer.Memory, cancellationToken);

                        // something is wrong so back off a bit
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                finally
                {
                    this.lastBatchFlush = DateTime.UtcNow;
                }
            }
        }

        private async Task<MemoryOwner<TDataItem>> TakeAllAsync(CancellationToken cancellationToken)
        {
            using var _ = await this.queueSemaphore.EnterAsync(cancellationToken);

            var buffer = MemoryOwner<TDataItem>.Allocate(this.queue.Count);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer.Span[i] = this.queue.Dequeue();
            }

            this.logger.LogTrace($"Took {buffer.Length} survivors from the queue");

            return buffer;
        }

        protected abstract Task<bool> ProcessAsync(ReadOnlyMemory<TDataItem> items, CancellationToken cancellationToken);
    }
}
