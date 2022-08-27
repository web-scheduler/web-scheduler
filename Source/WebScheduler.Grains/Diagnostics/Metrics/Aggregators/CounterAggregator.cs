namespace WebScheduler.Grains.Diagnostics.Metrics.Aggregators;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;

internal sealed class CounterAggregator
{
    private readonly KeyValuePair<string, object>[] tags;
    private long value = 0;
    public CounterAggregator() => this.tags = Array.Empty<KeyValuePair<string, object>>();

    public CounterAggregator(in TagList tagList)
    {
        if (tagList.Name1 == null)
        {
            this.tags = Array.Empty<KeyValuePair<string, object>>();
        }
        else if (tagList.Name2 == null)
        {
            this.tags = new[] { new KeyValuePair<string, object>(tagList.Name1, tagList.Value1) };
        }
        else if (tagList.Name3 == null)
        {
            this.tags = new[]
            {
                new KeyValuePair<string, object>(tagList.Name1, tagList.Value1),
                new KeyValuePair<string, object>(tagList.Name2, tagList.Value2)
            };
        }
        else if (tagList.Name4 == null)
        {
            this.tags = new[]
            {
                new KeyValuePair<string, object>(tagList.Name1, tagList.Value1),
                new KeyValuePair<string, object>(tagList.Name2, tagList.Value2),
                new KeyValuePair<string, object>(tagList.Name3, tagList.Value3)
            };
        }
        else
        {
            this.tags = new[]
            {
                new KeyValuePair<string, object>(tagList.Name1, tagList.Value1),
                new KeyValuePair<string, object>(tagList.Name2, tagList.Value2),
                new KeyValuePair<string, object>(tagList.Name3, tagList.Value3),
                new KeyValuePair<string, object>(tagList.Name4, tagList.Value4)
            };
        }
    }

    public long Value => this.value;

    public void Add(long measurement) => Interlocked.Add(ref this.value, measurement);

    public Measurement<long> Collect() => new(this.value, this.tags);
}
