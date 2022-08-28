namespace WebScheduler.Grains.Tests.Services;
using System;
using System.Threading.Tasks;
using WebScheduler.Abstractions.Services;
using Xunit;

using Microsoft.Extensions.Logging;
using Orleans;
using WebScheduler.Abstractions.Grains.Scheduler;
using System.Threading;
using WebScheduler.Grains.Services;
using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers;
using OpenTelemetry.Trace;
using static WebScheduler.Server.Constants.OpenTelemetryAttributeName;
using System.Collections.Concurrent;
using DotNext.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using FluentAssertions;
using System.Globalization;

public class BatchedPriorityConcurrentQueueWorker_Tests
{
    [Theory]
    [InlineData(10 /*maxCapacity*/, 1/*batchSize*/, 1/*concurrentFlows*/, 1/*initialQueueCapacitiy*/, "00:00:10" /*batchFlushInterval*/, 1/*expectedBatches*/, 1 /*number of items to create*/)]
    [InlineData(10_000, 500, 1, 1, "00:00:10", 1, 20)]
    public async Task BatchedPriorityConcurrentQueueWorker_Basic(int maxCapacity, int batchSize, int concurrentFlows, int initialQueueCapacity, string batchFlushInterval, int? expectedBatches, int numberOfItemsToCreate)
    {
        var expectedItems = 0;
        var capturedBatches = new ConcurrentBag<(DateTime CapturedTime, BatchQueueItem<string, BatchedPriorityQueuePriority>[] Items)>();

        var queue = new TestingBatchPriorityQueueWorker(maxCapacity, batchSize, concurrentFlows, initialQueueCapacity, TimeSpan.Parse(batchFlushInterval, CultureInfo.InvariantCulture), processCapturer: items =>
        {
            capturedBatches.Add((DateTime.Now, items.ToArray()));
            return true;
        });

        var cts = new CancellationTokenSource();
        await queue.StartAsync(cts.Token);

        var expectedData = new Dictionary<string, BatchQueueItem<string, BatchedPriorityQueuePriority>>();
        for (int i = 0; i < numberOfItemsToCreate; i++)
        {
            var value = new BatchQueueItem<string, BatchedPriorityQueuePriority>(Key: Guid.NewGuid().ToString(), Value: Guid.NewGuid().ToString(), Status: BatchedPriorityQueuePriority.High, Timestamp: () => DateTime.Now);
            expectedData.Add(value.Key, value);

            await queue.PostOneAsync(value, cts.Token);
        }

        expectedItems++;
        while (true)
        {
            // we've seen the expected number of items across all batches, our work is done.
            if (capturedBatches.Sum(c => c.Items.Length) == expectedItems)
            {
                break;
            }
        }
        await queue.StopAsync(cts.Token);

        if (expectedBatches.HasValue)
        {
            capturedBatches.Should().HaveCount(expectedBatches.Value);
        }

        capturedBatches.Sum(c => c.Items.Length).Should().Be(expectedItems);

        capturedBatches.SelectMany(b => b.Items).ToList().Should().BeEquivalentTo(expectedData.Values);
        //// push 10K random items
        //using (var buffer = Enumerable.Range(1, 10_000).Select(x => new DataItem(x, x.ToString(), (Status)Random.Shared.Next(2), DateTime.UtcNow)).ToMemoryOwner())
        //{
        //    await service.PostAsync(buffer.Memory);
        //}
    }


    /// <summary>
    /// Batch writes <see cref="ScheduledTaskTriggerHistory"/> records prioritizing trigger failures over successes.
    /// Thinking is failures are more valuable than sucesssful trigger deliveries.
    /// </summary>
    public class TestingBatchPriorityQueueWorker :
        BatchedPriorityConcurrentQueueWorker<BatchQueueItem<string, BatchedPriorityQueuePriority>, string, BatchedPriorityQueuePriority>
    {
        private readonly ILogger<ScheduledTaskTriggerHistoryGrainDataSaver> logger;
        private readonly IClusterClient clusterClient;
        private readonly IExceptionObserver exceptionObserver;

        public TestingBatchPriorityQueueWorker(int maxCapacity, int batchSize, int concurrentFlows, int initialQueueCapacity, TimeSpan batchFlushInterval, Func<ReadOnlyMemory<BatchQueueItem<string, BatchedPriorityQueuePriority>>, bool> processCapturer) : base(initialQueueCapacity)
        {
            this.MaxCapacity = maxCapacity;
            this.BatchSize = batchSize;
            this.ConcurrentFlows = concurrentFlows;
            this.BatchFlushInterval = batchFlushInterval;
            this.ProcessAsyncAction = processCapturer;
        }

        public Func<ReadOnlyMemory<BatchQueueItem<string, BatchedPriorityQueuePriority>>, bool> ProcessAsyncAction { get; }

        protected override Task<bool> ProcessAsync(ReadOnlyMemory<BatchQueueItem<string, BatchedPriorityQueuePriority>> items, CancellationToken cancellationToken) => Task.FromResult(this.ProcessAsyncAction(items));


    }

}
