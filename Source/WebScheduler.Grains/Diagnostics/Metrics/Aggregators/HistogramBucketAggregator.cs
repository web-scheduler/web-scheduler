namespace WebScheduler.Grains.Diagnostics.Metrics.Aggregators;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

internal class HistogramBucketAggregator
{
    private long _value = 0;
    private readonly KeyValuePair<string, object>[] _tags;
    public long Bound { get; }

    public HistogramBucketAggregator(KeyValuePair<string, object>[] tags, long bound, KeyValuePair<string, object> label)
    {
        this._tags = tags.Concat(new[] { label }).ToArray();
        this.Bound = bound;
    }

    public ReadOnlySpan<KeyValuePair<string, object>> Tags => this._tags;

    public long Value => this._value;

    public void Add(long measurement) => Interlocked.Add(ref this._value, measurement);

    public Measurement<long> Collect()
    {
        return new Measurement<long>(this._value, this._tags);
    }
}
