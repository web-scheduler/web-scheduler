namespace WebScheduler.Grains.Scheduler;
using Polly.Wrap;

/// <summary>
/// Holds Polly policies for grain storage.
/// </summary>
public interface ICommonGrainStoragePolicy
{
    /// <summary>
    /// The retry policy.
    /// </summary>
    AsyncPolicyWrap RetryPolicy { get; }

    /// <summary>
    /// The breaker and retry policy.
    /// </summary>
    AsyncPolicyWrap BreakerAndTimeout { get; }
}
