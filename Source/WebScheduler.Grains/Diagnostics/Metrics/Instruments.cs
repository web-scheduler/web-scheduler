namespace WebScheduler.Grains.Diagnostics.Metrics;
using System.Diagnostics.Metrics;

internal static class Instruments
{
    internal static readonly Meter Meter = new("WebScheduler");
}
