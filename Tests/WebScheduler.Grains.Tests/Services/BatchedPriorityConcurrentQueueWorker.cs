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
using CommunityToolkit.HighPerformance;

public class BatchedPriorityConcurrentQueueWorker_Tests
{
    [Theory]
    [InlineData(10 /*maxCapacity*/, 1/*batchSize*/, 1/*concurrentFlows*/, 1/*initialQueueCapacitiy*/, "00:00:10" /*batchFlushInterval*/, 1/*expectedBatches*/, 1 /*number of items to create*/)]
    [InlineData(10_000, 500, 1, 1, "00:00:10", 1, 20)]
    [InlineData(10_000, 50, 1, 1, "00:10:00", 10, 500)]
    public async Task BatchedPriorityConcurrentQueueWorker_Basic(int maxCapacity, int batchSize, int concurrentFlows, int initialQueueCapacity, string batchFlushInterval, int? expectedBatches, int numberOfItemsToCreate)
    {
        var capturedBatches = new ConcurrentBag<(DateTime CapturedTime, BatchQueueItem<string, BatchedPriorityQueuePriority>[] Items)>();

        var queue = new TestingBatchPriorityQueueWorker(maxCapacity, batchSize, concurrentFlows, initialQueueCapacity, TimeSpan.Parse(batchFlushInterval, CultureInfo.InvariantCulture), processCapturer: items =>
        {
            capturedBatches.Add((DateTime.Now, items.ToArray()));
            return true;
        });

        var cts = new CancellationTokenSource();
        await queue.StartAsync(cts.Token);

        var expectedData = new Dictionary<string, BatchQueueItem<string, BatchedPriorityQueuePriority>>();

        using (var buffer = Enumerable.Range(1, numberOfItemsToCreate).Select(x =>
        {
            var item = new BatchQueueItem<string, BatchedPriorityQueuePriority>(Key: Guid.NewGuid().ToString(), Value: Guid.NewGuid().ToString(), Status: (BatchedPriorityQueuePriority)Random.Shared.Next(2), Timestamp: () => DateTime.Now);
            expectedData.Add(item.Key, item);
            return item;
        }).ToMemoryOwner())
        {
            await queue.PostAsync(buffer.Memory);
        }

        await Task.Run(() =>
         {
             while (true)
             {
                 // we've seen the expected number of items across all batches, our work is done.
                 if (capturedBatches.Select(c=> c.Items.Length).Sum() == numberOfItemsToCreate)
                 {
                     break;
                 }
             }
             return Task.CompletedTask;
         });

        await queue.StopAsync(cts.Token);


        capturedBatches.Select(c => c.Items.Length).Sum().Should().Be(numberOfItemsToCreate);

        capturedBatches.SelectMany(b => b.Items).ToList().Should().BeEquivalentTo(expectedData.Values);
        if (expectedBatches.HasValue)
        {
            capturedBatches.Should().HaveCount(expectedBatches.Value);
        }
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
public static class MemoryExtensions
{
    public static IMemoryOwner<T> ToMemoryOwner<T>(this IEnumerable<T> items)
    {
        var buffer = new ArrayPoolBufferWriter<T>(items.TryGetNonEnumeratedCount(out var count) ? count : -2);
        foreach (var item in items)
        {
            buffer.Write(item);
        }
        return buffer;
    }
}
