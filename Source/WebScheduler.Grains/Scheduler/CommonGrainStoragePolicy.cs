namespace WebScheduler.Grains.Scheduler;
using Polly;
using Polly.Wrap;

/// <summary>
/// Holds policies for grain state.
/// </summary>
public class CommonGrainStoragePolicy : ICommonGrainStoragePolicy
{
    /// <inheritdoc/>
    public AsyncPolicyWrap RetryPolicy { get; }
    /// <inheritdoc/>
    public AsyncPolicyWrap BreakerAndTimeout { get; }

    /// <summary>
    /// ctor
    /// </summary>
    public CommonGrainStoragePolicy()
    {
        var retry = Policy.Handle<Exception>()
            .WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, _, retryCount, context) => context.GetLogger().ErrorWritingStateRetry(exception, context.GetGrainKey(), retryCount));
        var breaker = Policy.Handle<Exception>().CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));
        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(20));
        this.RetryPolicy = Policy.WrapAsync(retry, breaker, timeout);
        this.BreakerAndTimeout = Policy.WrapAsync(breaker, timeout);
    }
}
