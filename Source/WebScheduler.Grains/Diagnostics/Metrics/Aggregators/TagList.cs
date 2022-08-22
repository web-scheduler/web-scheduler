namespace WebScheduler.Grains.Diagnostics.Metrics.Aggregators;

internal record struct TagList(
    string Name1,
    object Value1,
    string Name2 = default,
    object Value2 = default,
    string Name3 = default,
    object Value3 = default,
    string Name4 = default,
    object Value4 = default);
