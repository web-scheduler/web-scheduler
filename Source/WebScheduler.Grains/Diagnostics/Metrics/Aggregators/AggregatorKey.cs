namespace WebScheduler.Grains.Diagnostics.Metrics.Aggregators;
using System;
using System.Collections.Generic;
using System.Linq;

internal readonly struct AggregatorKey : IEquatable<AggregatorKey>
{
    public AggregatorKey(string instrumentName, KeyValuePair<string, object>[] tags)
    {
        this.InstrumentName = instrumentName;
        this.Tags = tags;
    }
    public string InstrumentName { get; }
    public KeyValuePair<string, object>[] Tags { get; }

    public override int GetHashCode() => HashCode.Combine(this.InstrumentName, this.Tags);
    public bool Equals(AggregatorKey other) => this.InstrumentName == other.InstrumentName && this.Tags.SequenceEqual(other.Tags);

    public override bool Equals(object obj) => obj is AggregatorKey key && this.Equals(key);
}
