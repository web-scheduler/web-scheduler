namespace Polly;

using Microsoft.Extensions.Logging;

internal static class ContextExtensions
{
    private const string LoggerKey = "LoggerKey";
    private const string GrainKey = "GrainKey";

    /// <summary>
    /// Adds the logger to the Pollcy Context.
    /// </summary>
    /// <param name="context">The Context.</param>
    /// <param name="logger">The Logger.</param>
    /// <returns>The Context.</returns>
    public static Context WithLogger(this Context context, ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    /// <summary>
    /// Gets the logger from the Polly Context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>The logger.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ILogger GetLogger(this Context context)
    {
        if (context.TryGetValue(LoggerKey, out var logger))
        {
            return (ILogger)logger;
        }
        throw new ArgumentNullException(LoggerKey);
    }

    /// <summary>
    /// Adds the grain key to the Pollcy Context.
    /// </summary>
    /// <param name="context">The Context.</param>
    /// <param name="grainKey">The grain key.</param>
    /// <returns>The Context.</returns>
    public static Context WithGrainKey(this Context context, string grainKey)
    {
        context[GrainKey] = grainKey;
        return context;
    }

    /// <summary>
    /// Gets the grain key from the Polly Context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>The grain key.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetGrainKey(this Context context)
    {
        if (context.TryGetValue(GrainKey, out var grainKey))
        {
            return (string)grainKey;
        }
        throw new ArgumentNullException(GrainKey);
    }
}
