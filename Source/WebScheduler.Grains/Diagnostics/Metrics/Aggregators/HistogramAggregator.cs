namespace WebScheduler.Grains.Diagnostics.Metrics.Aggregators;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

internal class HistogramAggregator
{
    private readonly KeyValuePair<string, object>[] _tags;
    private readonly HistogramBucketAggregator[] _buckets;
    private long _count;
    private long _sum;

    public HistogramAggregator(long[] buckets, KeyValuePair<string, object>[] tags, Func<long, KeyValuePair<string, object>> getLabel)
    {
        if (buckets[^1] != long.MaxValue)
        {
            buckets = buckets.Concat(new[] { long.MaxValue }).ToArray();
        }

        this._tags = tags;
        this._buckets = buckets.Select(b => new HistogramBucketAggregator(tags, b, getLabel(b))).ToArray();
    }

    public void Record(long number)
    {
        int i;
        for (i = 0; i < this._buckets.Length; i++)
        {
            if (number <= this._buckets[i].Bound)
            {
                break;
            }
        }
        this._buckets[i].Add(1);
        Interlocked.Increment(ref this._count);
        Interlocked.Add(ref this._sum, number);
    }

    public IEnumerable<Measurement<long>> CollectBuckets()
    {
        foreach (var bucket in this._buckets)
        {
            yield return bucket.Collect();
        }
    }

    public Measurement<long> CollectCount() => new(this._count, this._tags);

    public Measurement<long> CollectSum() => new(this._sum, this._tags);
}
