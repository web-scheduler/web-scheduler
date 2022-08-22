using System.Diagnostics.Metrics;

namespace WebScheduler.Grains.Diagnostics.Metrics;

internal static class Instruments
{
    internal static readonly Meter Meter = new("WebScheduler");
}
